using System;

namespace Sfsp
{
    public abstract class SfspAsyncTransfer
    {
        protected object locker = new object();

        protected long progress = 0;

        /// <summary>
        /// Evento sollevato ad ogni cambio di stato
        /// </summary>
        public event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Solleva l'evento StatusChanged
        /// </summary>
        /// <param name="status">Nuovo stato</param>
        private void OnStatusChange(TransferStatus status)
        {
            if (StatusChanged != null)
                StatusChanged(this, new TransferStatusChangedEventArgs(status));
        }

        /// <summary>
        /// Modifica lo stato attuale del trasferimento e solleva il relativo evento di passaggio di stato
        /// </summary>
        /// <param name="status"></param>
        protected void SetStatus(TransferStatus status)
        {
            Status = status;
            OnStatusChange(status);
        }

        protected TransferStatus _status;
        /// <summary>
        /// Stato attuale del trasferimento
        /// </summary>
        public TransferStatus Status
        {
            get
            {
                TransferStatus to_ret;
                lock (locker)
                {
                    to_ret = _status;
                }
                return to_ret;
            }
            protected set
            {
                lock (locker)
                {
                    _status = value;
                }
            }
        }

        /// <summary>
        /// Dimensione effettiva in byte del trasferimento (sono inclusi solo i file)
        /// </summary>
        public long TotalSize
        {
            get;
            protected set;
        }

        /// <summary>
        /// Interrompe il treasferimento
        /// </summary>
        public abstract void Abort();
    }
}
