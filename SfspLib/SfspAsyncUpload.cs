using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
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

        private Thread uploadThread;

        /// <summary>
        /// Inizializza un nuovo trasferimento (in stato "New").
        /// </summary>
        /// <param name="basePath">Directory "radice" contenente gli oggetti da trasferire</param>
        /// <param name="relativePaths">Elenco di tutti gli oggetti da trasferire, espressi come percorsi relativi rispetto alla radice</param>
        internal SfspAsyncUpload(string basePath, List<string> relativePaths)
        {
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

        /// <summary>
        /// Avvia l'upload di tutti gli oggetti selezionati per il trasferimento
        /// </summary>
        public void Start()
        {
            uploadThread = new Thread(UploadTask);
            uploadThread.Start();
        }

        private void UploadTask()
        {

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
