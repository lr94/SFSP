using System;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic.ApplicationServices;

namespace SfspClient
{
    class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private App _application;
        private ReadOnlyCollection<string> _commandLineArgs;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            // Primo avvio
            _commandLineArgs = eventArgs.CommandLine;
            _application = new App();
            _application.Run();

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Avvii successivi
            base.OnStartupNextInstance(eventArgs);
            _commandLineArgs = eventArgs.CommandLine;

            string[] args = new string[_commandLineArgs.Count];
            _commandLineArgs.CopyTo(args, 0);

            _application.SecondInstance(args);
        }
    }
}
