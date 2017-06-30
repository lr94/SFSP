using System;

namespace Sfsp
{
    public class TransferRequestEventArgs : EventArgs
    {
        internal TransferRequestEventArgs(SfspAsyncDownload download)
        {
            Download = download;
        }

        public SfspAsyncDownload Download
        {
            get;
            private set;
        }
    }
}
