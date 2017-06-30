using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using Sfsp.Messaging;

namespace Sfsp
{
    public class SfspAsyncDownload : ISfspAsyncTransfer
    {
        private IList<string> relativePaths;

        private object locker = new object();

        internal SfspAsyncDownload(SfspRequestMessage request)
        {
            TotalSize = (long)request.TotalSize;
            relativePaths = request.RelativePaths;
        }

        /// <summary>
        /// Restituisce una lista di tutti gli oggetti che l'host remoto vuole inviare
        /// </summary>
        /// <returns>Lista contenente i percorsi relativi dei file e delle cartelle</returns>
        public List<String> GetObjects()
        {
            List<string> list = new List<string>(relativePaths);
            return list;
        }

        private long _Progress;
        public long Progress
        {
            get
            {
                long to_ret;
                lock(locker)
                {
                    to_ret = _Progress;
                }
                return to_ret;
            }
            private set
            {
                lock(locker)
                {
                    _Progress = value;
                }
            }
        }

        TransferStatus _Status;
        public TransferStatus Status
        {
            get
            {
                TransferStatus to_ret;
                lock(locker)
                {
                    to_ret = _Status;
                }
                return to_ret;
            }
            private set
            {
                lock(locker)
                {
                    _Status = value;
                }
            }
        }

        public long TotalSize
        {
            get;
            private set;
        }

        public event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
