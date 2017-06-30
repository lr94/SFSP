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

        public long Progress
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TransferStatus Status
        {
            get
            {
                throw new NotImplementedException();
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
