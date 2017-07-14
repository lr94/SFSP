using System;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta gli argomenti dell'evento sollevato quando viene trovato un nuovo host durante la scansione
    /// </summary>
    public class SfspHostFoundEventArgs : EventArgs
    {
        public SfspHostFoundEventArgs(SfspHost host)
        {
            Host = host;
        }

        /// <summary>
        /// Host rilevato
        /// </summary>
        public SfspHost Host
        {
            get;
            private set;
        }
    }
}
