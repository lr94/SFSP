using System;

namespace Sfsp
{
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
