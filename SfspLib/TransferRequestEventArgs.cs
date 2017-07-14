using System;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta gli argomenti dell'evento TransferRequest.
    /// Incapsula un oggetto Download in stato pending
    /// </summary>
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
