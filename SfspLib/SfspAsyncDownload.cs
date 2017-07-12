using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using Sfsp.Messaging;
using Sfsp.TcpUtils; // Per extension method TcpClient.GetState()

namespace Sfsp
{
    public class SfspAsyncDownload : SfspAsyncTransfer
    {
        private TcpClient tcpClient;
        Thread downloadThread;

        private bool response_sent = false;

        internal SfspAsyncDownload(SfspRequestMessage request, TcpClient client)
        {
            TotalSize = (long)request.TotalSize;
            relativePaths = request.RelativePaths;
            RemoteHostName = request.RemoteHostName;
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
            this.FailureException = new TransferAbortException(TransferAbortException.AbortType.LocalAbort);
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

            DestinationDirectory = destinationPath;

            downloadThread = new Thread(() => DownloadTask(destinationPath));
            downloadThread.IsBackground = true;
            downloadThread.Start();
        }

        private void DownloadTask(string destinationPath)
        {
            SfspNetworkStream stream = null;

            try
            {
                stream = new SfspNetworkStream(tcpClient.Client);

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
                        if (!DownloadFile(stream, fullPath, (long)createFileMsg.FileSize))
                            toReceive.Add(fileRelativePath); // Se non va a buon fine rimetto il file tra quelli ancora da fare
                    }
                }

                ForceProgressUpdate();
                SetStatus(TransferStatus.Completed);
            }
            catch(Exception ex)
            {
                this.FailureException = ex;
                SetStatus(TransferStatus.Failed);
            }
            finally
            {
                if(stream != null)
                    stream.Close();

                tcpClient.Close();
            }
        }
        
        private bool DownloadFile(NetworkStream stream, string fullPath, long size)
        {
            string tmpFullPath = fullPath + ".part";
            FileStream fs = File.Open(tmpFullPath, FileMode.Create, FileAccess.Write);

            bool okFlag = true;

            try
            {
                // Preparazione per il calcolo del checksum
                SHA256 sha256 = SHA256.Create();
                sha256.Initialize();

                byte[] buffer = new byte[BUFFER_SIZE];
                long fReceived = 0;
                while (fReceived < size)
                {
                    if (Aborting)
                        throw new TransferAbortException(TransferAbortException.AbortType.LocalAbort);

                    // Riceve i dati e li inserisce nel buffer
                    int bufSize = (size - fReceived) < BUFFER_SIZE ? (int)(size - fReceived) : BUFFER_SIZE;
                    int n = stream.Read(buffer, 0, bufSize);

                    // Se non ho ricevuto dati, verifico lo stato della connessione TCP
                    if (n == 0)
                    {
                        TcpState state = tcpClient.GetState();
                        // Se è stata chiusa sollevo un'eccezione
                        if (state != TcpState.Established)
                            throw new TransferAbortException(TransferAbortException.AbortType.RemoteAbort);
                    }

                    // Scrive il buffer su disco
                    fs.Write(buffer, 0, n);

                    // Calcolo del checksum
                    sha256.TransformBlock(buffer, 0, n, buffer, 0);

                    fReceived += n;
                    progress += n;

                    // Eventuale aggiornamento dell'avanzamento dell'operazione
                    ProgressUpdateIfNeeded();
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
                {
                    // Tutto a posto
                    // Se c'è già un file con lo stesso nome lo elimino
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);

                    // Rinomino il file temporaneo
                    File.Move(tmpFullPath, fullPath);

                    confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Ok);
                }
                else
                {
                    // Qualcosa è andato storto, elimino il file temporaneo
                    File.Delete(tmpFullPath);

                    okFlag = false;

                    progress -= size;
                    // Aggiornamento dell'avanzamento dell'operazione
                    ForceProgressUpdate();

                    confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Error);
                }
                confirm.Write(stream);
            }
            finally
            {
                fs.Close();
            }

            return okFlag;
        }

        /// <summary>
        /// Percorso in cui si sta andando a salvare la cartella o i file ricevuti
        /// </summary>
        public string DestinationDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Nome SFSP dell'host che invia i dati
        /// </summary>
        public string RemoteHostName
        {
            get;
            private set;
        }

        /// <summary>
        /// Ottiene l'indirizzo IP dell'host remoto
        /// </summary>
        public override IPAddress RemoteAddress
        {
            get
            {
                IPEndPoint ip_ep = this.tcpClient.Client.RemoteEndPoint as IPEndPoint;
                if (ip_ep == null)
                    return null;

                return ip_ep.Address;
            }
        }
    }
}
