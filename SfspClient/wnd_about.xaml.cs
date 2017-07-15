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
    /// Logica di interazione per wnd_about.xaml
    /// </summary>
    public partial class wnd_about : Window
    {
        public wnd_about()
        {
            InitializeComponent();

            txtb_version.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
