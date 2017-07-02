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

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per wnd_transfers.xaml
    /// </summary>
    public partial class wnd_transfers : Window
    {
        #region "NotifyIcon"
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        private void InitNotifyIcon(String iconName, String menuKey)
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/" + iconName)).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = true;

            notifyIcon.Click += (object sender, EventArgs e) => this.Show();
            notifyIcon.MouseDown += (object sender, System.Windows.Forms.MouseEventArgs e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ContextMenu menu = (ContextMenu)this.FindResource(menuKey);
                    menu.IsOpen = true;
                }
            };
        }
        #endregion

        public wnd_transfers()
        {
            InitializeComponent();

            InitNotifyIcon("icon.ico", "notifyIconMenu");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lst_transfers.Items.Add("Hello");
        }
    }
}
