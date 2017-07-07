using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Sfsp.Messaging;

namespace Sfsp
{
    public class SfspAsyncDownload : SfspAsyncTransfer
    {
        private IList<string> relativePaths;
        private TcpClient tcpClient;
        Thread downloadThread;

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

            progress = 0;
            SetStatus(TransferStatus.InProgress);

            // Elengo degli oggetti da ricevere (si ridurrà mano a mano che li riceviamo)
            List<string> toReceive = new List<string>(relativePaths);

            while (toReceive.Count > 0)
            {
                SfspMessage msg = SfspMessage.ReadMessage(stream);

                // Se vogliamo creare una directory
                if (msg is SfspCreateDirectoryMessage)
                {
                    // Ottengo il percorso relativo della cartella da creare
                    SfspCreateDirectoryMessage createDirMsg = (SfspCreateDirectoryMessage)msg;
                    string dirRelativePath = createDirMsg.RelativePath;

                    // Rimuovo questo oggetto dalla lista degli oggetti da scaricare
                    if (!toReceive.Remove(dirRelativePath))
                        throw new ProtocolViolationException("Unexpected directory " + dirRelativePath);

                    // La vado a creare
                    string fullPath = Path.Combine(destinationPath, PathUtils.ConvertSFSPPathToOS(dirRelativePath));
                    Directory.CreateDirectory(fullPath);

                    // Invio conferma
                    SfspConfirmMessage confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Ok);
                    confirm.Write(stream);
                }
                else if (msg is SfspCreateFileMessage)
                {
                    // Ottengo il percorso relativo del file da creare
                    SfspCreateFileMessage createFileMsg = (SfspCreateFileMessage)msg;
                    string fileRelativePath = createFileMsg.FileRelativePath;

                    // Rimuovo questo oggetto dalla lista degli oggetti da scaricare
                    if (!toReceive.Remove(fileRelativePath))
                        throw new ProtocolViolationException("Unexpected file " + fileRelativePath);

                    // Scarico il file
                    string fullPath = Path.Combine(destinationPath, PathUtils.ConvertSFSPPathToOS(fileRelativePath));
                    DownloadFile(stream, fullPath, (long)createFileMsg.FileSize);
                }
            }

            SetStatus(TransferStatus.Completed);
        }

        private void DownloadFile(NetworkStream stream, string fullPath, long size)
        {
            FileStream fs = File.Open(fullPath, FileMode.Create, FileAccess.Write);

            // Preparazione per il calcolo del checksum
            SHA256 sha256 = SHA256.Create();
            sha256.Initialize();

            byte[] buffer = new byte[1024];
            long fReceived = 0;
            while(fReceived < size)
            {
                // Riceve i dati e li inserisce nel buffer
                int bufSize = (size - fReceived) < 1024 ? (int)(size - fReceived) : 1024;
                int n = stream.Read(buffer, 0, bufSize);
                // Scrive il buffer su disco
                fs.Write(buffer, 0, n);

                // Calcolo del checksum
                sha256.TransformBlock(buffer, 0, n, buffer, 0);

                fReceived += n;
                progress += n;
            }
            fs.Close();

            // Verifica del checksum
            sha256.TransformFinalBlock(buffer, 0, 0);
            byte[] hash = sha256.Hash;
            SfspMessage receivedMsg = SfspMessage.ReadMessage(stream);
            if (!(receivedMsg is SfspChecksumMessage))
                throw new ProtocolViolationException("Unexpected SFSP message");
            SfspChecksumMessage checksumMsg = (SfspChecksumMessage)receivedMsg;

            SfspConfirmMessage confirm;
            if (checksumMsg.Check(hash))
                confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Ok);
            else
            {
                progress -= size;
                confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Error);
            }
            confirm.Write(stream);
        }

        /// <summary>
        /// Restituisce una lista di tutti gli oggetti che l'host remoto vuole inviare.
        /// I percorsi sono con separatore SFSP ("\")
        /// </summary>
        /// <returns>Lista contenente i percorsi relativi dei file e delle cartelle</returns>
        public List<String> GetObjects()
        {
            List<string> list = new List<string>(relativePaths);
            return list;
        }     


        public override void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
