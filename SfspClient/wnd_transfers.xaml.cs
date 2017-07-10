﻿using System;
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
            // Nome dell'host come da impostazioni
            hostConfiguration = new SfspHostConfiguration(appSettings.HostName);

            // Inizializzo il listener
            listener = new SfspListener(hostConfiguration);
            listener.TransferRequest += Listener_TransferRequest;
            // Mi metto in ascolto
            listener.Start();
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
                
                foreach(SfspHost h in hostList)
                {
                    SfspAsyncUpload upload = h.Send(path, hostConfiguration);
                    // Aggiorno stato dell'avanzamento 10 volte al secondo
                    upload.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 100);

                    // Aggiunto in cima alla lista
                    var wrapper = new TransferWrapper(upload, h.Name, System.IO.Path.GetFileName(path));
                    transfer_wrapper_list.Insert(0, wrapper);

                    upload.Start();
                }
            }
        }

        public void Quit()
        {
            int running_transfer = transfer_wrapper_list.Where(tw =>
                (tw.TransferObject.Status != TransferStatus.Failed && tw.TransferObject.Status != TransferStatus.Completed)).Count();
            if(running_transfer > 0)
            {
                MessageBoxResult res =  MessageBox.Show("Ci sono " + running_transfer.ToString() + " trasferimenti in corso, uscendo verranno interrotti. Vuoi veramente uscire?", "Conferma", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
            }

            notifyIcon.Visible = false;
            Application.Current.Shutdown();
        }

        private void Listener_TransferRequest(object sender, TransferRequestEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SfspAsyncDownload download = e.Download;

                wnd_incomingfile dialog = new wnd_incomingfile(download, appSettings.DefaultPath);

                bool? result;
                if (appSettings.AutoAccept)
                    result = true;
                else
                    result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    // Aggiorno stato dell'avanzamento 10 volte al secondo
                    download.ProgressUpdateTime = new TimeSpan(0, 0, 0, 0, 100);

                    // Aggiunto in cima alla lista
                    var wrapper = new TransferWrapper(download, e.Download.RemoteHostName, download.GetObjects()[0]);
                    transfer_wrapper_list.Insert(0, wrapper);

                    download.Accept(dialog.DestinationPath);
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
            wnd_hosts hostScannerDialog = new wnd_hosts("PROVA", hostConfiguration);

            bool? result = hostScannerDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // Invio file
                List<SfspHost> h = hostScannerDialog.GetSelectedHosts();
                MessageBox.Show(h.Count.ToString());
            }
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
    }
}
