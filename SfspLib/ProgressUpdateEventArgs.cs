using System;

namespace Sfsp
{
    /// <summary>
    /// Argomento dell'evento sollevato ad ogni aggiornamento dell'avanzamento
    /// </summary>
    public class ProgressUpdateEventArgs : EventArgs
    {
        internal ProgressUpdateEventArgs(long progress, long total, long speed)
        {
            Progress = progress;
            TotalSize = total;
            Speed = speed;
        }

        /// <summary>
        /// Numero di byte trasferiti
        /// </summary>
        public long Progress
        {
            get;
            private set;
        }

        /// <summary>
        /// Dimensione (B) totale del trasferimento
        /// </summary>
        public long TotalSize
        {
            get;
            private set;
        }

        /// <summary>
        /// Velocità (B/s) calcolata sull'ultimo intervallo temporale
        /// </summary>
        public long Speed
        {
            get;
            private set;
        }
    }
}
