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
        Thread downloadThread;

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

            // Setto lo stato
            SetStatus(TransferStatus.Failed);

            // Invio il messaggio di rifiuto
            SfspConfirmMessage confirmMsg = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Error);
            confirmMsg.Write(tcpClient.GetStream());
        }

        /// <summary>
        /// Accetta la richiesta di trasferimento
        /// </summary>
        /// <param name="destinationPath">Percorso in cui salvare i dati ricevuti</param>
        public void Accept(string destinationPath)
        {
            lock (locker)
            {
                if (response_sent)
                    throw new InvalidOperationException("Response already sent");
                response_sent = true;
            }

            if (!Directory.Exists(destinationPath))
                throw new DirectoryNotFoundException();

            downloadThread = new Thread(() => DownloadTask(destinationPath));
            downloadThread.Start();
        }

        private void DownloadTask(string destinationPath)
        {
            NetworkStream stream = tcpClient.GetStream();

            // Invio il messaggio di accettazione
            SfspConfirmMessage confirmMsg = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Ok);
            confirmMsg.Write(stream);

            Progress = 0;
            Status = TransferStatus.InProgress;

            // Elengo degli oggetti da ricevere (si ridurrà mano a mano che li riceviamo)
            List<string> toReceive = new List<string>(relativePaths);

            while(toReceive.Count > 0)
            {
                SfspMessage msg = SfspMessage.ReadMessage(stream);

                // Se vogliamo creare una directory
                if(msg is SfspCreateDirectoryMessage)
                {
                    // Ottengo il percorso relativo della cartella da creare
                    SfspCreateDirectoryMessage createDirMsg = (SfspCreateDirectoryMessage)msg;
                    string dirRelativePath = createDirMsg.RelativePath;

                    // Rimuovo questo oggetto dalla lista degli oggetti da scaricare
                    toReceive.Remove(dirRelativePath);

                    // La vado a creare
                    string fullPath = Path.Combine(destinationPath, dirRelativePath);
                    Directory.CreateDirectory(fullPath);
                }
                else if(msg is SfspCreateFileMessage)
                {
                    // Ottengo il percorso relativo del file da creare
                    SfspCreateFileMessage createFileMsg = (SfspCreateFileMessage)msg;
                    string fileRelativePath = createFileMsg.FileRelativePath;

                    // Rimuovo questo oggetto dalla lista degli oggetti da scaricare
                    toReceive.Remove(fileRelativePath);

                    // Scarico il file
                    string fullPath = Path.Combine(destinationPath, fileRelativePath);
                    DownloadFile(stream, fullPath, (long)createFileMsg.FileSize);
                }
            }
        }

        private void DownloadFile(NetworkStream stream, string fullPath, long size)
        {
            FileStream fs = File.Open(fullPath, FileMode.Create, FileAccess.Write);

            byte[] buffer = new byte[1024];
            long fReceived = 0;
            while(fReceived < size)
            {
                // Riceve i dati e li inserisce nel buffer
                int n = stream.Read(buffer, 0, 1024);
                // Scrive il buffer su disco
                fs.Write(buffer, 0, n);

                fReceived += n;
            }
            fs.Close();

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
        
        private void OnStatusChange(TransferStatus status)
        {
            if (StatusChanged != null)
                StatusChanged(this, new TransferStatusChangedEventArgs(status));
        }

        private void SetStatus(TransferStatus status)
        {
            Status = status;
            OnStatusChange(status);
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
