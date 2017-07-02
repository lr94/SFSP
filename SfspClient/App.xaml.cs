using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SfspClient
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            wnd_transfers wnd1 = new wnd_transfers();

            String[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                if (args[1] == "-hidden")
                    wnd1.Hide();
                else
                    wnd1.Show();
            }
            else
                wnd1.Show();
        }
    }
}
