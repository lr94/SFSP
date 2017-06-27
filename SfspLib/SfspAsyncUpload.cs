using System;

namespace Sfsp
{
    public class SfspAsyncUpload : ISfspAsyncTransfer
    {
        internal SfspAsyncUpload()
        {
            this.Status = TransferStatus.New;
        }

        public event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        public void Start()
        {

        }

        public long Progress
        {
            get;
            private set;
        }

        public TransferStatus Status
        {
            get;
            private set;
        }

        public long TotalSize
        {
            get;
            private set;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
