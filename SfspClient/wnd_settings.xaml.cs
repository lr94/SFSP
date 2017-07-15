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
using System.Net;

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per wnd_settings.xaml
    /// </summary>
    public partial class wnd_settings : Window
    {
        private ApplicationSettings settings;

        public wnd_settings()
        {
            InitializeComponent();

            settings = new ApplicationSettings();
            txt_name.Text = settings.HostName;
            txt_path.Text = settings.DefaultPath;
            chk_autoaccept.IsChecked = settings.AutoAccept;
            rd_online.IsChecked = (settings.Mode == ApplicationSettings.HostMode.Online);
            rd_offline.IsChecked = (settings.Mode == ApplicationSettings.HostMode.Offline);

            txt_multicast.Text = settings.MulticastAddress.ToString();
            chk_loopback.IsChecked = settings.Loopback;
            txt_udp_port.Text = settings.UdpPort.ToString();
            txt_tcp_port.Text = settings.TcpPort.ToString();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            settings.HostName = txt_name.Text;
            settings.DefaultPath = txt_path.Text;
            settings.AutoAccept = chk_autoaccept.IsChecked.Value;
            if (rd_online.IsChecked.Value)
                settings.Mode = ApplicationSettings.HostMode.Online;
            else if (rd_offline.IsChecked.Value)
                settings.Mode = ApplicationSettings.HostMode.Offline;

            // è per forza valido, l'utente non potrebbe cliccare su Ok altrimenti
            IPAddress multicastAddress = IPAddress.Parse(txt_multicast.Text);
           
            bool loopback = chk_loopback.IsChecked.Value;
            int udpPort = Int32.Parse(txt_udp_port.Text);
            int tcpPort = Int32.Parse(txt_tcp_port.Text);

            if(!multicastAddress.Equals(settings.MulticastAddress) || loopback != settings.Loopback || udpPort != settings.UdpPort || tcpPort != settings.TcpPort)
            {
                settings.MulticastAddress = multicastAddress;
                settings.Loopback = loopback;
                settings.UdpPort = udpPort;
                settings.TcpPort = tcpPort;

                MessageBox.Show("Sono state modificate le impostazioni di rete.\nAffinché le modifiche abbiano effetto è necessario riavviare manualmente l'applicazione.");
            }

            settings.Store();
            this.Close();
        }

        private void btn_browse_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();

            if (System.IO.Directory.Exists(txt_path.Text))
                fbd.SelectedPath = txt_path.Text;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txt_path.Text = fbd.SelectedPath;
        }

        private void txt_path_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_ok.IsEnabled = CheckIfValid();
        }

        bool CheckIfValid()
        {
            if (txt_name.Text.Trim() == "")
                return false;

            ushort port;
            if (!UInt16.TryParse(txt_udp_port.Text, out port))
                return false;
            if (port == 0)
                return false;
            if (!UInt16.TryParse(txt_tcp_port.Text, out port))
                return false;
            if (port == 0)
                return false;

            IPAddress address;
            if (!IPAddress.TryParse(txt_multicast.Text, out address))
                return false;

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !address.IsIPv6Multicast)
                return false;

            if(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte first = address.GetAddressBytes()[0];
                // I primi 4 bit di un indirizzo IPv4 multicast sono sempre 1110
                if (first >> 4 != 14)
                    return false;
            }

            if (!System.IO.Directory.Exists(txt_path.Text))
                return false;

            return true;
        }

        private void txt_name_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_ok.IsEnabled = CheckIfValid();
        }

        private void txt_multicast_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_ok.IsEnabled = CheckIfValid();
        }

        private void txt_udp_port_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_ok.IsEnabled = CheckIfValid();
        }

        private void txt_tcp_port_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_ok.IsEnabled = CheckIfValid();
        }
    }
}
