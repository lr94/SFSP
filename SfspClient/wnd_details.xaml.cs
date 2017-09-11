using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Logica di interazione per wnd_details.xaml
    /// </summary>
    public partial class wnd_details : Window
    {
        private TransferWrapper tw;

        internal wnd_details(TransferWrapper transferWrapper)
        {
            InitializeComponent();

            tw = transferWrapper;
            this.DataContext = transferWrapper;

            // Lista dei percorsi relativi (uno per riga)
            txt_list.Text = tw.TransferObject.RelativePaths.Aggregate((a, b) => a + "\n" + b);
        }
    }
}
