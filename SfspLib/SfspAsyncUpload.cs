using System;
using System.IO;
using System.Collections.Generic;
using Sfsp.Messaging;

namespace Sfsp
{
    public class SfspAsyncUpload : ISfspAsyncTransfer
    {
        private object locker = new object();

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

        private long _progress;
        public long Progress
        {
            get
            {
                long to_ret;
                lock(locker)
                {
                    to_ret = _progress;
                }
                return to_ret;
            }
            private set
            {
                lock(locker)
                {
                    _progress = value;
                }
            }
        }

        TransferStatus _status;
        public TransferStatus Status
        {
            get
            {
                TransferStatus to_ret;
                lock(locker)
                {
                    to_ret = _status;
                }
                return to_ret;
            }
            private set
            {
                lock(locker)
                {
                    _status = value;
                }
            }
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
