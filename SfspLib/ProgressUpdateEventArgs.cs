using System;

namespace Sfsp
{
    public class ProgressUpdateEventArgs : EventArgs
    {
        internal ProgressUpdateEventArgs(long progress, long total, double speed)
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
        /// Velocità (B/s) calcolata sull'ultimo intervallo
        /// </summary>
        public double Speed
        {
            get;
            private set;
        }
    }
}
