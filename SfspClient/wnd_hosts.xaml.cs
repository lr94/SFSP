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
    /// Logica di interazione per wnd_hosts.xaml
    /// </summary>
    public partial class wnd_hosts : Window
    {
        public wnd_hosts()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lst_hosts.Items.Add("Boh");
            lst_hosts.Items.Add("Boh");
        }
    }
}
