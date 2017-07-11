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
using Sfsp;

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per wnd_incomingfile.xaml
    /// </summary>
    public partial class wnd_incomingfile : Window
    {
        public wnd_incomingfile(SfspAsyncDownload download, string defaultPath)
        {
            InitializeComponent();

            txt_path.Text = defaultPath;
            txtb_name.Text = String.Format(txtb_name.Text, download.RemoteHostName);
            txtb_filename.Text = String.Format(txtb_filename.Text, download.RelativePaths[0]);
            txtb_size.Text = String.Format(txtb_size.Text, NumericFormatter.FormatBytes(download.TotalSize));
        }

        public string DestinationPath
        {
            get
            {
                return txt_path.Text;
            }
        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btn_browse_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();

            if(System.IO.Directory.Exists(txt_path.Text))
                fbd.SelectedPath = txt_path.Text;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txt_path.Text = fbd.SelectedPath;
        }
    }
}
