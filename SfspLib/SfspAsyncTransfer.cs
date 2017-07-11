using System;
using System.Collections.Generic;

namespace Sfsp
{
    public abstract class SfspAsyncTransfer
    {
        public const int BUFFER_SIZE = 1024;

        protected object locker = new object();

        /// <summary>
        /// Numero di bytes trasferiti
        /// </summary>
        protected long progress = 0;

        /// <summary>
        /// Lista degli oggetti (percorsi relativi)
        /// </summary>
        protected List<string> relativePaths;

        /// <summary>
        /// Evento sollevato ad ogni cambio di stato
        /// </summary>
        public event EventHandler<TransferStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Evento sollevato a intervalli regolari (vedi proprietà ProgressUpdateTime) e ogni
        /// volta che termina il trasferimento di un file
        /// </summary>
        public event EventHandler<ProgressUpdateEventArgs> ProgressUpdate;

        /// <summary>
        /// Numero di bytes trasferiti all'ultima invocazione dell'evento ProgressUpdate
        /// </summary>
        private long notified_progress = 0;
        /// <summary>
        /// Istante dell'ultima invocazione dell'evento ProgressUpdate
        /// </summary>
        private DateTime last_progress_update = new DateTime(0);
        /// <summary>
        /// Se è specificato un intervallo di aggiornamento non nullo e se è trascorso almeno tale
        /// intervallo dall'ultimo aggiornamento, lancia l'evento di aggiornamento dell'avanzamento
        /// del trasferimento (sì, l'ho scritto sul serio).
        /// </summary>
        protected void ProgressUpdateIfNeeded()
        {
            long update_time_ticks = ProgressUpdateTime.Ticks;
            
            // Se l'intervallo è nullo non lanciamo l'evento
            if (update_time_ticks == 0)
                return;

            DateTime now = DateTime.Now;

            // Se non è trascorso l'intervallo esco
            if (now.Ticks - last_progress_update.Ticks < update_time_ticks)
                return;

            ForceProgressUpdate();
        }

        /// <summary>
        /// Lancia l'evento di aggiornamento dell'avanzamento del trasferimento.
        /// </summary>
        protected void ForceProgressUpdate()
        {
            DateTime now = DateTime.Now;

            // Calcolo la velocità
            double seconds = now.Subtract(last_progress_update).TotalSeconds;
            long speed = (long)((double)(progress - notified_progress) / seconds);
            // (se un file viene ritrasferito in seguito a errore risulterebbe velocità negativa)
            if (speed < 0)
                speed = 0;

            last_progress_update = now;
            notified_progress = progress;

            // Sollevo l'evento
            if (ProgressUpdate != null)
                ProgressUpdate(this, new ProgressUpdateEventArgs(progress, TotalSize, speed));
        }

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

        protected TimeSpan _ProgressUpdateTime = new TimeSpan(0);
        /// <summary>
        /// Intervallo di attivazione dell'evento ProgressUpdate. Impostare un intervallo nullo per non
        /// temporizzare l'attivazione dell'evento (viene attivato solo dopo che un file è stato caricato).
        /// 
        /// Questa proprietà non può essere modificata durante il trasferimento
        /// </summary>
        public TimeSpan ProgressUpdateTime
        {
            get
            {
                return _ProgressUpdateTime;
            }
            set
            {
                lock(locker)
                {
                    if(_status != TransferStatus.New && _status != TransferStatus.Pending)
                        throw new InvalidOperationException("Cannot change update time during the transfer");
                }

                _ProgressUpdateTime = value;
            }
        }

        /// <summary>
        /// Lista dei percorsi relativi degli oggetti da trasferire
        /// </summary>
        public IReadOnlyList<string> RelativePaths
        {
            get
            {
                if (relativePaths == null)
                    return null;
                return relativePaths.AsReadOnly();
            }
        }

        /// <summary>
        /// Se il trasferimento è fallito (Status == Failed) contiene l'eccezione che ne ha causato il fallimento.
        /// Null altrimenti
        /// </summary>
        public Exception FailureException
        {
            get;
            internal set;
        }

        private bool _Aborting = false;
        /// <summary>
        /// True se vogliamo interrompere il trasferimento
        /// </summary>
        protected bool Aborting
        {
            get
            {
                bool value;
                lock (locker)
                {
                    value = _Aborting;
                }
                return value;
            }
            set
            {
                lock (locker)
                {
                    _Aborting = value;
                }
            }
        }

        /// <summary>
        /// Interrompe il trasferimento in corso chiudendo la connessione con l'host remoto
        /// </summary>
        public void Abort()
        {
            Aborting = true;
        }
    }
}
