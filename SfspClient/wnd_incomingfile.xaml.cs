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
        private ISet<string> active_objects;
        SfspAsyncDownload download;

        public wnd_incomingfile(SfspAsyncDownload download, string defaultPath, ISet<string> activeObjects)
        {
            InitializeComponent();

            active_objects = activeObjects;
            this.download = download;

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

        /// <summary>
        /// Restituisce true se il percorso di destinazione specificato causa conflitti con altri trasferimenti
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        private bool CheckForConflicts(string destinationPath)
        {
            IReadOnlyCollection<string> relativePaths = download.RelativePaths;
            foreach (string relativePath in relativePaths)
            {
                string hypotheticalLocalPath = System.IO.Path.Combine(destinationPath, relativePath.Replace('\\', System.IO.Path.DirectorySeparatorChar));

                if (active_objects.Contains(hypotheticalLocalPath))
                    return true;
            }

            return false;
        }

        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            if(CheckForConflicts(txt_path.Text))
            {
                MessageBox.Show("Il percorso di destinazione selezionato causa un conflitto con altri trasferimenti in corso");
                return;
            }

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

        private void txt_path_TextChanged(object sender, TextChangedEventArgs e)
        {
            btn_accept.IsEnabled = System.IO.Directory.Exists(txt_path.Text);
        }
    }
}
