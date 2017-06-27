using System;

namespace Sfsp
{
    public class TransferStatusChangedEventArgs : EventArgs
    {
        public TransferStatusChangedEventArgs(TransferStatus newStatus)
        {
            NewStatus = newStatus;
        }

        public TransferStatus NewStatus
        {
            get;
            private set;
        }
    }
}
