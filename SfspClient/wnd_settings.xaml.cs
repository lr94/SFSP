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

            settings.Store();
            this.Close();
        }
    }
}
