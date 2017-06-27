using System;

namespace SFSP
{
    public class SfspHostFoundEventArgs : EventArgs
    {
        public SfspHostFoundEventArgs(SfspHost host)
        {
            Host = host;
        }

        public SfspHost Host
        {
            get;
            private set;
        }
    }
}
