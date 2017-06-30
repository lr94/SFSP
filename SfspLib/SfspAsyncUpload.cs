using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
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
                return;
            }

            // Se arriviamo qui l'invio è stato accettato!
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
