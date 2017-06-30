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
        private TcpClient tcpClient;

        private object locker = new object();

        private bool response_sent = false;

        internal SfspAsyncDownload(SfspRequestMessage request, TcpClient client)
        {
            TotalSize = (long)request.TotalSize;
            relativePaths = request.RelativePaths;
            tcpClient = client;

            Status = TransferStatus.Pending;
        }

        /// <summary>
        /// Rifiuta la richiesta di trasferimento
        /// </summary>
        public void Deny()
        {
            lock(locker)
            {
                if (response_sent)
                    throw new InvalidOperationException("Response already sent");
                response_sent = true;
            }

            // Invio il messaggio di rifiuto
            SfspConfirmMessage confirmMsg = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Error);
            confirmMsg.Write(tcpClient.GetStream());
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
        /// <summary>
        /// Numero di byte già trasferiti (vengono contati solo i byte di contenuto dei file da trasferire)
        /// </summary>
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
        /// <summary>
        /// Stato attuale del trasferimento
        /// </summary>
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
