using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

using Sfsp;

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per wnd_transfers.xaml
    /// </summary>
    public partial class wnd_transfers : Window
    {
        #region "NotifyIcon"
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        bool still_running_tip_shown = false;

        private void InitNotifyIcon(String iconName, String menuKey)
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/" + iconName)).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = true;

            notifyIcon.MouseDown += (object sender, System.Windows.Forms.MouseEventArgs e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ContextMenu menu = (ContextMenu)this.FindResource(menuKey);
                    menu.IsOpen = true;
                }
                else if(e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    this.Show();
                    this.Activate();
                }
            };
        }
        #endregion

        ApplicationSettings appSettings;
        SfspHostConfiguration hostConfiguration;
        SfspListener listener;

        ObservableCollection<TransferWrapper> transfer_wrapper_list;

        void InitSfsp()
        {
            try
            {
                // Nome dell'host come da impostazioni
                hostConfiguration = new SfspHostConfiguration(appSettings.HostName);

                // Inizializzo il listener
                listener = new SfspListener(hostConfiguration);
                listener.TransferRequest += Listener_TransferRequest;
                // Mi metto in ascolto
                listener.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Si è verificato un errore durante l'inizializzazione:\n" + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        public wnd_transfers()
        {
            InitializeComponent();

            InitNotifyIcon("icon.ico", "notifyIconMenu");

            // Carico le impostazioni dell'applicazione
            appSettings = new ApplicationSettings();

            // Inizializzo la lista dei trasferimenti
            transfer_wrapper_list = new ObservableCollection<TransferWrapper>();
            lst_transfers.ItemsSource = transfer_wrapper_list;

            // Inizializzo tutto ciò che è necessario per il protocollo SFSP
            InitSfsp();
        }

        /// <summary>
        /// Ottiene l'insieme dei percorsi assoluti di tutti gli oggetti (file e cartelle) coinvolti nei trasferimenti (sia upload che download o solo download)
        /// attualmente in corso.
        /// </summary>
        /// <param name="onlyDownloads">Se true vengono considerati solo gli oggetti in download</param>
        /// <returns></returns>
        public ISet<string> GetActiveObjects(bool onlyDownloads = false)
        {
            var list = transfer_wrapper_list.SelectMany(tw =>
            {
                SfspAsyncTransfer transfer = tw.TransferObject;
                if (transfer.Status != TransferStatus.InProgress)
                    return new List<string>();

                if(onlyDownloads && !(transfer is SfspAsyncDownload))
                    return new List<string>();

                return transfer.RelativePaths.Select(relativePath =>
                {
                    string localBase;
                    if (transfer is SfspAsyncDownload)
                        localBase = ((SfspAsyncDownload)transfer).DestinationDirectory;
                    else if (transfer is SfspAsyncUpload)
                        localBase = ((SfspAsyncUpload)transfer).BaseDirectory;
                    else
                        localBase = ""; // Non verrà mai eseguito

                    relativePath = relativePath.Replace('\\', System.IO.Path.DirectorySeparatorChar);

                    return System.IO.Path.Combine(localBase, relativePath);
                });
            }).ToList();
            
            return new SortedSet<string>(list);
        }

        /// <summary>
        /// Da chiamare quando si vuole condividere un file o una cartella. Aprirà la finestra di scansione ecc ecc 
        /// </summary>
        /// <param name="path">Percorso dell'oggetto da condividere</param>
        public void ShareObject(string path)
        {
            wnd_hosts hostScannerDialog = new wnd_hosts(path, hostConfiguration);

            bool? result = hostScannerDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // Invio file
                List<SfspHost> hostList = hostScannerDialog.GetSelectedHosts();

                bool checkedForConflicts = false; // True se abbiamo già controllato se ci sono conflitti
                
                foreach(SfspHost h in hostList)
                {
                    SfspAsyncUpload upload = h.Send(path, hostConfiguration);

                    // Da eseguire solo col primo host (tanto con quelli dopo sarebbe uguale)
                    if(!checkedForConflicts)
                    {
                        // Controllo che nessuno dei file che andiamo a caricare sia tra i file che stiamo scaricando
                        IReadOnlyCollection<string> objectsToUpload = upload.RelativePaths;
                        ISet<string> activeDownloadObjects = GetActiveObjects(true);
                        foreach(string currentObject in objectsToUpload)
                        {
                            string fullPath = System.IO.Path.Combine(upload.BaseDirectory, currentObject.Replace('\\', System.IO.Path.DirectorySeparatorChar));
                            if(activeDownloadObjects.Contains(fullPath))
                            {
                                MessageBox.Show("Impossibile inviare l'elemento selezionato a causa di un conflitto con altri elementi in download");
                                return;
                            }
                        }

                        checkedForConflicts = true;
                    }

                    // Aggiorno stato dell'avanzamento 10 volte al secondo
                    upload.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 100);

                    // Aggiunto in cima alla lista
                    var wrapper = new TransferWrapper(upload, h.Name);
                    transfer_wrapper_list.Insert(0, wrapper);

                    upload.Start();
                }
            }
        }

        public void Quit()
        {
            int running_transfers = transfer_wrapper_list.Where(tw =>
                (tw.TransferObject.Status != TransferStatus.Failed && tw.TransferObject.Status != TransferStatus.Completed)).Count();

            if (running_transfers > 0)
            {
                MessageBoxResult res =  MessageBox.Show("Ci sono " + running_transfers.ToString() + " trasferimenti in corso, uscendo verranno interrotti. Vuoi veramente uscire?", "Conferma", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
            }

            foreach (var tw in transfer_wrapper_list)
            {
                if (tw.TransferObject.Status != TransferStatus.Failed && tw.TransferObject.Status != TransferStatus.Completed)
                    tw.TransferObject.Abort();
            }

            notifyIcon.Visible = false;
            Application.Current.Shutdown();
        }

        private void Listener_TransferRequest(object sender, TransferRequestEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SfspAsyncDownload download = e.Download;

                // Percorso in cui andare a salvare i file ricevuti
                string destinationPath = appSettings.DefaultPath;

                // Lista degli oggetti attivi (per evitare conflitti)
                ISet<string> active_objects = GetActiveObjects();
                
                wnd_incomingfile dialog = new wnd_incomingfile(download, destinationPath, active_objects);

                // True: trasferimento accettato, false o nessun valore: trasferimento da rifiutare
                bool? result;

                // Se è impostata l'accettazione automatica
                if (appSettings.AutoAccept)
                {
                    // Verifica che non ci siano conflitti
                    bool conflicts = false;

                    // Scorro tutti gli oggetti che dovremmo ricevere...
                    IReadOnlyCollection<string> relativePaths = download.RelativePaths;
                    foreach (string relativePath in relativePaths)
                    {
                        // ...ne determino l'ipotetico percorso assoluto
                        string hypotheticalLocalPath = System.IO.Path.Combine(destinationPath, relativePath.Replace('\\', System.IO.Path.DirectorySeparatorChar));

                        // Se è già usato da un oggetto attivo abbiamo un conflitto
                        if (active_objects.Contains(hypotheticalLocalPath))
                        {
                            conflicts = true;
                            break;
                        }
                    }

                    // Se ci sono conflitti mi comporto come se non fosse impostata l'accettazione automatica
                    if (conflicts)
                    {
                        result = dialog.ShowDialog();
                        destinationPath = dialog.DestinationPath;
                    }
                    else
                        result = true; // Nessun conflitto, accetto e basta
                }
                else // Se non è impostata l'accettazione automatica
                {
                    // Chiedo all'utente
                    result = dialog.ShowDialog();
                    destinationPath = dialog.DestinationPath;
                }

                // Il trasferimento s'ha da fare?
                if (result.HasValue && result.Value)
                {
                    // Aggiorno stato dell'avanzamento 10 volte al secondo
                    download.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 100);

                    // Aggiungo in cima alla lista
                    var wrapper = new TransferWrapper(download, e.Download.RemoteHostName);
                    transfer_wrapper_list.Insert(0, wrapper);

                    download.Accept(destinationPath);
                }
                else
                    download.Deny();
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // lst_transfers.Items.Add("Hello");
        }

        private void mnu_about_Click(object sender, RoutedEventArgs e)
        {
            wnd_about about = new wnd_about();
            about.ShowDialog();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnu_settings_Click(object sender, RoutedEventArgs e)
        {
            wnd_settings settings = new wnd_settings();
            settings.ShowDialog();

            // Aggiorno impostazioni
            appSettings.Load();
            hostConfiguration.Name = appSettings.HostName;
            hostConfiguration.Online = (appSettings.Mode == ApplicationSettings.HostMode.Online);
        }

        private void mnu_transfers_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;

            if(!still_running_tip_shown)
            {
                notifyIcon.ShowBalloonTip(200,"Sfsp","SfspClient è ancora in esecuzione", System.Windows.Forms.ToolTipIcon.Info);
                still_running_tip_shown = true;
            }
        }

        private void mnu_quit_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        private void mnu_clear_Click(object sender, RoutedEventArgs e)
        {
            // Rimuovo i trasferimenti completati o falliti dalla lista
            var toRemove = transfer_wrapper_list.Where(tw =>
                (tw.TransferObject.Status == TransferStatus.Failed || tw.TransferObject.Status == TransferStatus.Completed)).ToList();
            
            foreach(TransferWrapper tw in toRemove)
                transfer_wrapper_list.Remove(tw);
        }

        private void mnu_share_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Tutti i file|*.*";

            if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ShareObject(ofd.FileName);
        }

        private void mnu_stop_Click(object sender, RoutedEventArgs e)
        {
            TransferWrapper tw = lst_transfers.SelectedItem as TransferWrapper;
            if (tw != null)
                tw.TransferObject.Abort();
        }

        private void mnu_delete_Click(object sender, EventArgs e)
        {
            TransferWrapper tw = lst_transfers.SelectedItem as TransferWrapper;
            if (tw != null)
                transfer_wrapper_list.Remove(tw);
        }

        private void mnu_transferinfo_Click(object sender, EventArgs e)
        {
            TransferWrapper tw = lst_transfers.SelectedItem as TransferWrapper;
            if (tw != null)
            {
                wnd_details details = new wnd_details(tw);
                details.DataContext = tw;
                details.Show();
            }
        }

        private void lst_transfers_Drop(object sender, DragEventArgs e)
        {
            icn_upload.Visibility = Visibility.Collapsed;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if(files.Length == 1)
                    ShareObject(files[0]);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length == 1)
                    icn_upload.Visibility = Visibility.Visible;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            icn_upload.Visibility = Visibility.Collapsed;
        }

        private void item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelected();
        }

        private void item_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                TransferWrapper tw = lst_transfers.SelectedItem as TransferWrapper;
                if (tw != null && (tw.TransferObject.Status == TransferStatus.Completed || tw.TransferObject.Status == TransferStatus.Failed))
                    transfer_wrapper_list.Remove(tw);
            }
            else if(e.Key == Key.Enter)
            {
                OpenSelected();
            }
        }

        private void OpenSelected()
        {
            TransferWrapper tw = lst_transfers.SelectedItem as TransferWrapper;

            if (tw != null)
            {
                SfspAsyncDownload transfer = tw.TransferObject as SfspAsyncDownload;
                if (transfer != null && transfer.Status == TransferStatus.Completed)
                {
                    string path = System.IO.Path.Combine(transfer.DestinationDirectory, transfer.RelativePaths[0].Replace('\\', System.IO.Path.DirectorySeparatorChar));
                    System.Diagnostics.Process.Start(path);
                }
            }
        }

        private void mnu_serverinfo_Click(object sender, RoutedEventArgs e)
        {
            string udpList = listener.UdpLocalEndpoints.Select(ep => ep.ToString()).Aggregate((a, b) => a + "\n" + b);
            string tcp = listener.TcpLocalEndpoint.ToString();

            string message = "Endpoint locale ascolto TCP:\n" + tcp + "\n\nEndpoint locali ascolto UDP:\n" + udpList + "\n\nGruppo multicast:\n" + hostConfiguration.MulticastAddress.ToString();
            MessageBox.Show(message, "Informazioni server", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
