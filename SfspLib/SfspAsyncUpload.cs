using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Sfsp.Messaging;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta il trasferimento verso un altro host di un insieme di oggetti (file e cartelle)
    /// </summary>
    public class SfspAsyncUpload : ISfspAsyncTransfer
    {
        private object locker = new object();

        private string basePath;
        private List<string> relativePaths;
        private SfspHost remoteHost;

        private Thread uploadThread;

        private bool started = false;

        /// <summary>
        /// Inizializza un nuovo trasferimento (in stato "New").
        /// </summary>
        /// <param name="basePath">Directory "radice" contenente gli oggetti da trasferire</param>
        /// <param name="relativePaths">Elenco di tutti gli oggetti da trasferire, espressi come percorsi relativi rispetto alla radice</param>
        internal SfspAsyncUpload(SfspHost remoteHost, string basePath, List<string> relativePaths)
        {
            this.remoteHost = remoteHost;
            this.relativePaths = relativePaths;
            this.basePath = basePath;

            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException("Cannot find base directory " + basePath + ".");

            if (relativePaths.Count == 0)
                throw new ArgumentException("Cannot send an empty file set", "relativePaths");

            this.Status = TransferStatus.New;
        }

        /// <summary>
        /// Evento sollevato ad ogni cambio di stato
        /// </summary>
        public event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        private void OnStatusChange(TransferStatus status)
        {
            if (StatusChanged != null)
                StatusChanged(this, new TransferStatusChangedEventArgs(status));
        }

        /// <summary>
        /// Avvia l'upload di tutti gli oggetti selezionati per il trasferimento
        /// </summary>
        public void Start()
        {
            // Verifichiamo che l'oggetto non sia già stato utilizzato per un trasferimento
            // uso lock perché altrimenti in linea teorica un altro thread potrebbe chiamare
            // Start() mentre questo si trova tra if(started) e started = true.
            lock(locker)
            {
                if (started)
                    throw new InvalidOperationException("The transfer has already been started");

                started = true;
            }

            uploadThread = new Thread(UploadTask);
            uploadThread.Start();
        }

        private void UploadTask()
        {
            // Scorro tutti gli oggetti da inviare
            long totalSize = 0;
            foreach(string currentObject in relativePaths)
            {
                // Ne ottengo il percorso completo per poi determinarne la dimensione, che aggiungo a totalSize
                String fullpath = Path.Combine(basePath, currentObject);
                if(File.Exists(fullpath))
                {
                    FileInfo fi = new FileInfo(fullpath);
                    totalSize += fi.Length;
                }
            }
            TotalSize = totalSize;

            // Mi collego all'host remoto
            TcpClient client = remoteHost.CreateConnection();
            NetworkStream stream = client.GetStream();

            // Preparo il messaggio di richiesta da inviare
            SfspRequestMessage request = new SfspRequestMessage((ulong)totalSize, relativePaths);

            // Invio il messaggio
            request.Write(stream);
            SetStatus(TransferStatus.Pending);
            
            // Leggo il messaggio di risposta
            SfspMessage msg = SfspMessage.ReadMessage(stream);

            // Ci aspettiamo una risposta di tipo Confirm!
            if(!(msg is SfspConfirmMessage))
            {
                SetStatus(TransferStatus.Failed);
                return;
            }

            SfspConfirmMessage confirm = (SfspConfirmMessage)msg;
            // Se l'invio è stato rifiutato...
            if(confirm.Status == SfspConfirmMessage.FileStatus.Error)
            {
                SetStatus(TransferStatus.Failed);
                stream.Close();
                client.Close();
                return;
            }

            // Se arriviamo qui l'invio è stato accettato!
            SetStatus(TransferStatus.InProgress);

            // Inizio il trasferimento vero e proprio
            foreach(String objectRelativePath in relativePaths)
            {
                // Percorso completo sul sistema locale
                string fullPath = Path.Combine(basePath, objectRelativePath);

                // Se è una cartella...
                if (Directory.Exists(fullPath))
                {
                    // ...inviamo un comando di creazione della cartella
                    SfspCreateDirectoryMessage createDirMessage = new SfspCreateDirectoryMessage(objectRelativePath);
                    createDirMessage.Write(stream);

                    // Attendo conferma
                    msg = SfspMessage.ReadMessage(stream);
                    if (!(msg is SfspConfirmMessage))
                    {
                        SetStatus(TransferStatus.Failed);
                        stream.Close();
                        client.Close();
                        return;
                    }
                    confirm = (SfspConfirmMessage)msg;
                    if(confirm.Status != SfspConfirmMessage.FileStatus.Ok)
                    {
                        SetStatus(TransferStatus.Failed);
                        stream.Close();
                        client.Close();
                        return;
                    }
                }
                // Se è un file...
                else if (File.Exists(fullPath))
                {
                    // Provo massimo 3 volte ad inviarlo
                    bool done = false;
                    int attempts = 3;
                    do
                    {
                        done |= UploadFile(stream, fullPath, objectRelativePath);
                        attempts--;
                    } while (!done && attempts > 0);
                    if(!done)
                    {
                        SetStatus(TransferStatus.Failed);
                        stream.Close();
                        client.Close();
                        return;
                    }
                }
                else
                    throw new FileNotFoundException("File or directory not found", fullPath);
            }

            // Abbiamo finito
            SetStatus(TransferStatus.Completed);
            stream.Close();
            client.Close();
        }

        private bool UploadFile(NetworkStream stream, string fullPath, string relativePath)
        {
            // Determino la dimensione del file
            FileInfo fInfo = new FileInfo(fullPath);
            long fSize = fInfo.Length;
            // Invio il comando di creazione del file
            SfspCreateFileMessage createFileMessage = new SfspCreateFileMessage((ulong)fSize, relativePath);
            createFileMessage.Write(stream);

            // Apro in lettura il file
            FileStream fStream = File.OpenRead(fullPath);

            // Preparazione per il calcolo del checksum
            SHA256 sha256 = SHA256.Create();
            sha256.Initialize();

            // Invio i dati
            byte[] buffer = new byte[1024];
            long fSent = 0;
            while(fSent < fSize)
            {
                int bufSize = (fSize - fSent < 1024) ? (int)(fSize - fSent) : 1024;
                // Leggo dal file
                bufSize = fStream.Read(buffer, 0, bufSize);
                // Invio i dati
                stream.Write(buffer, 0, bufSize);
                // Calcolo del checksum
                sha256.TransformBlock(buffer, 0, bufSize, buffer, 0);
                // Aggiorno i contatori
                fSent += bufSize;
                Progress += bufSize;
            }
            fStream.Close();

            // Invio checksum
            sha256.TransformFinalBlock(buffer, 0, 0);
            byte[] hash = sha256.Hash;
            SfspChecksumMessage checksumMsg = new SfspChecksumMessage(hash);
            checksumMsg.Write(stream);

            // Aspetto una risposta
            SfspMessage receivedMsg = SfspMessage.ReadMessage(stream);
            if (!(receivedMsg is SfspConfirmMessage))
                throw new ProtocolViolationException("Unexpected Sfsp message");
            SfspConfirmMessage confirmMsg = (SfspConfirmMessage)receivedMsg;

            // Il trasferimento è fallito, faccio retrocedere il contatore e restituisco false
            if(confirmMsg.Status != SfspConfirmMessage.FileStatus.Ok)
            {
                Progress -= fSize;
                return false;
            }

            // Tutto a posto
            return true;
        }

        private void SetStatus(TransferStatus status)
        {
            Status = status;
            OnStatusChange(status);
        }

        private long _progress;
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

        /// <summary>
        /// Numero totale dei byte da trasferire (inteso come "dimensione di tutti i file messi insieme", le cartelle non contano)
        /// </summary>
        public long TotalSize
        {
            get;
            private set;
        }

        /// <summary>
        /// Interrompe il trasferimento in corso chiudendo la connessione con l'host remoto
        /// </summary>
        public void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
