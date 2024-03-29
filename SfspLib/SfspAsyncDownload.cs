﻿using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using Sfsp.Messaging;
using Sfsp.TcpUtils; // Per extension method TcpClient.GetState() e per SfspNetworkStream

namespace Sfsp
{
    /// <summary>
    /// Rappresenta un download dall'host remoto
    /// </summary>
    public class SfspAsyncDownload : SfspAsyncTransfer
    {
        private TcpClient tcpClient;
        Thread downloadThread;

        private bool response_sent = false;

        /// <summary>
        /// Genera una nova istanza di un oggetto SfspAsyncDownload
        /// </summary>
        /// <param name="request">Messaggio di richiesta ricevuto dall'host remoto</param>
        /// <param name="client">TcpClient relativo alla connessione con l'host remoto</param>
        internal SfspAsyncDownload(SfspRequestMessage request, TcpClient client)
        {
            TotalSize = (long)request.TotalSize;
            relativePaths = request.RelativePaths;
            RemoteHostName = request.RemoteHostName;
            tcpClient = client;

            // Recupero l'IP remoto in modo che resti accessibile anche dopo la disconnessione
            IPEndPoint remote_ep = client.Client.RemoteEndPoint as IPEndPoint;
            if(remote_ep != null)
                _RemoteAddress = remote_ep.Address;

            // Il trasferimento è in attesa di accettazione
            Status = TransferStatus.Pending;
        }

        /// <summary>
        /// Rifiuta la richiesta di trasferimento
        /// </summary>
        public void Deny()
        {
            lock(locker)
            {
                // Se era già stata inviata una risposta lancio un'eccezione
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

        /// <summary>
        /// Routine principale del thread che si occupa di effettuare il download vero e proprio.
        /// Demanda il download dei file alla DownloadFile()
        /// </summary>
        /// <param name="destinationPath">Percorso di destinazione locale</param>
        private void DownloadTask(string destinationPath)
        {
            SfspNetworkStream stream = null;

            try
            {
                /* 
                    Uso SfspNetworkStream al posto di NetworkStream
                    NetworkStream funzionerebbe lo stesso, ma SfspMessage non potrebbe accorgersi
                    della disconnessione dell'host remoto (perché NetworkStream non permette di accedere
                    all'oggetto Socket sottostante)
                */
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

                // Il download è terminato, quindi forzo un aggiornamento dell'avanzamento
                ForceProgressUpdate();
                // E modifico lo stato
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

        /// <summary>
        /// Scarica un file e lo salva in locale.
        /// Finché il download è in corso i dati vengono memorizzati in un file temporaneo ".part"
        /// Se esiste già un file in fullPath, esso viene sovrascritto al termine.
        /// Può sollevare un'eccezione TransferAbortException uno dei due host interrompe la connessione
        /// </summary>
        /// <param name="stream">NetworkStream (va bene anche SfspNetworkStream) relativo alla connessione</param>
        /// <param name="fullPath">Percorso completo e definitivo del file locale</param>
        /// <param name="size">Dimensione del file da scaricare</param>
        /// <returns>True in caso di successo, False in caso di fallimento</returns>
        private bool DownloadFile(NetworkStream stream, string fullPath, long size)
        {
            // Apro in scrittura il file temporaneo
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
                    // Se l'utente ha chiesto di annullare sollevo un'eccezione per sospendere tutto
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

                // Conferma da inviare
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

                    // Aggiornamento dell'avanzamento dell'operazione (torniamo indietro)
                    progress -= size;
                    ForceProgressUpdate();

                    confirm = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Error);
                }
                // Invio la conferma
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

        private IPAddress _RemoteAddress;
        /// <summary>
        /// Ottiene l'indirizzo IP dell'host remoto
        /// </summary>
        public override IPAddress RemoteAddress
        {
            get
            {
                return _RemoteAddress;
            }
        }
    }
}
