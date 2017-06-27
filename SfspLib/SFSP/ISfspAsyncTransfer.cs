using System;

namespace SFSP
{
    public interface ISfspAsyncTransfer
    {
        event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        TransferStatus Status
        {
            get;
        }

        long TotalSize
        {
            get;
        }

        long Progress
        {
            get;
        }

        void Abort();
    }
}
