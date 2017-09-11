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

        /// <summary>
        /// Handler dell'evento sollevato all'avvio della prima istanza dell'applicazione
        /// </summary>
        /// <param name="e">Argomenti dell'evento</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Finestra principale dell'applicazione
            main_wnd = new wnd_transfers();

            String[] args = Environment.GetCommandLineArgs();

            // Gestione delle opzioni (notare che -hidden e -share non possono essere usati assieme)
            if (args.Length > 1)
            {
                // Il flag -hidden indica che l'applicazione deve essere avviata
                // con la finestra principale nascosta
                if (args[1] == "-hidden")
                    main_wnd.Hide();
                else
                    main_wnd.Show();

                // -share indica che si vuole subito condividere un file
                if (args[1] == "-share" && args.Length > 2)
                {
                    main_wnd.Show();
                    main_wnd.Activate();
                    main_wnd.Focus();
                    main_wnd.ShareObject(args[2]);
                }
            }
            else
                // Nessuna richiesta particolare, mostra la finestra principale e basta
                main_wnd.Show();
        }

        /// <summary>
        /// Chiamato quando si prova ad avviare una seconda istanza
        /// </summary>
        /// <param name="args">Array degli argomenti (escluso il nome del programma)</param>
        public void SecondInstance(string[] args)
        {
            // Mostro la finestra principale
            main_wnd.Show();
            main_wnd.Activate();
            main_wnd.Focus();

            // Se la seconda istanza è stata avviata per condividere un file
            if (args.Length > 1 && args[0] == "-share")
            {
                // Condivido il file
                main_wnd.ShareObject(args[1]);
            }
        }
    }
}
