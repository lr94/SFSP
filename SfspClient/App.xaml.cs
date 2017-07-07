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
        private wnd_transfers main_wnd;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            main_wnd = new wnd_transfers();

            String[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                if (args[1] == "-hidden")
                    main_wnd.Hide();
                else
                    main_wnd.Show();

                if (args[1] == "-share" && args.Length > 2)
                    main_wnd.ShareObject(args[2]);
            }
            else
                main_wnd.Show();
        }

        public void SecondInstance(string[] args)
        {
            if (args.Length > 2 && args[1] == "-share")
                main_wnd.ShareObject(args[2]);
        }
    }
}
