using System;
using System.IO;
using System.Collections.Generic;
using Sfsp.Messaging;

namespace Sfsp
{
    public class SfspAsyncUpload : ISfspAsyncTransfer
    {
        private string basePath;
        private List<string> relativePaths;

        internal SfspAsyncUpload(string basePath, List<string> relativePaths)
        {
            this.relativePaths = relativePaths;
            this.basePath = basePath;

            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException("Cannot find base directory " + basePath + ".");

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
