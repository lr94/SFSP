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
using Sfsp;

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per wnd_hosts.xaml
    /// </summary>
    public partial class wnd_hosts : Window
    {
        private SfspScanner scanner;
        private ObservableCollection<SfspHost> hosts;

        public wnd_hosts(string fileToSend, SfspHostConfiguration config)
        {
            InitializeComponent();

            string filename = System.IO.Path.GetFileName(fileToSend);
            txtb_filename.Text = String.Format(txtb_filename.Text, filename);

            hosts = new ObservableCollection<SfspHost>();
            lst_hosts.ItemsSource = hosts;

            scanner = new SfspScanner(config);
            scanner.HostFound += scanner_HostFound;
            scanner.ScanComplete += scanner_ScanComplete;
            scanner.Error += (object sender, Exception e) => {
                MessageBox.Show("Si è verificato un errore durante la scansione.\n" + e.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.Invoke(() => this.Close());
            };
            icn_spinner.Spin = true;
            scanner.StartScan(new TimeSpan(0, 0, 2));
        }

        public List<SfspHost> GetSelectedHosts()
        {
            List<SfspHost> selectedHosts = lst_hosts.SelectedItems.Cast<SfspHost>().ToList();
            return selectedHosts;
        }

        private void scanner_HostFound(object sender, SfspHostFoundEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                hosts.Add(e.Host);
            });
        }

        private void scanner_ScanComplete(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                icn_spinner.Spin = false;
                icn_spinner.Visibility = Visibility.Hidden;
                lst_hosts.IsEnabled = true;
            });
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void lst_hosts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si può cliccare su "Invia" solo se è selezionato almeno un host
            btn_send.IsEnabled = (lst_hosts.SelectedItems.Count > 0);
        }
    }
}
