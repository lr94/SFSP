using System;
using System.Collections.Generic;
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

        ApplicationSettings settings;
        SfspHostConfiguration hostConfiguration;
        SfspListener listener;

        void InitSfsp()
        {
            // Nome dell'host come da impostazioni
            hostConfiguration = new SfspHostConfiguration(settings.HostName);

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
            settings = new ApplicationSettings();

            // Inizializzo tutto ciò che è necessario per il protocollo SFSP
            InitSfsp();
        }

        /// <summary>
        /// Da chiamare quando si vuole condividere un file o una cartella. Aprirà la finestra di scansione ecc ecc 
        /// </summary>
        /// <param name="path">Percorso dell'oggetto da condividere</param>
        public void ShareObject(string path)
        {
            wnd_hosts hosts = new wnd_hosts();
            hosts.ShowDialog();
            MessageBox.Show(path);
        }

        private void Listener_TransferRequest(object sender, TransferRequestEventArgs e)
        {
            // Debug
            MessageBox.Show("Richiesta ricevuta: " + e.Download.GetObjects()[0]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lst_transfers.Items.Add("Hello");
        }

        private void mnu_about_Click(object sender, RoutedEventArgs e)
        {
            wnd_about about = new wnd_about();
            about.ShowDialog();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            wnd_hosts hosts = new wnd_hosts();
            hosts.Show();
        }

        private void mnu_settings_Click(object sender, RoutedEventArgs e)
        {
            wnd_settings settings = new wnd_settings();
            settings.ShowDialog();
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
                still_running_tip_shown = true;
                notifyIcon.ShowBalloonTip(1500, "Sfsp Cliet", "Sfsp Client è ancora in esecuzione", System.Windows.Forms.ToolTipIcon.Info);
            }
        }
    }
}
