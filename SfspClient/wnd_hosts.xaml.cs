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

            hosts = new ObservableCollection<SfspHost>();
            lst_hosts.ItemsSource = hosts;

            scanner = new SfspScanner(config);
            scanner.HostFound += scanner_HostFound;
            scanner.ScanComplete += scanner_ScanComplete;
            icn_spinner.Spin = true;
            scanner.StartScan(new TimeSpan(0, 0, 1));
        }

        public List<SfspHost> GetSelectedHosts()
        {
            List<SfspHost> selectedHosts = lst_hosts.SelectedItems.Cast<SfspHost>().ToList();
            return selectedHosts;
        }

        private void scanner_HostFound(object sender, SfspHostFoundEventArgs e)
        {
            Dispatcher.Invoke(new AddHostDelegate(AddHost), new object[] { e.Host });
        }

        private void scanner_ScanComplete(object sender, EventArgs e)
        {
            Dispatcher.Invoke(ScanComplete);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // lst_hosts.Items.Add("test");
        }

        private delegate void AddHostDelegate(SfspHost host);
        private void AddHost(SfspHost host)
        {
            hosts.Add(host);
        }

        private void ScanComplete()
        {
            icn_spinner.Spin = false;
            icn_spinner.Visibility = Visibility.Hidden;
            lst_hosts.IsEnabled = true;
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
