using System;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta l'argomento dell'evento sollevato quando un trasferimento cambia stato
    /// </summary>
    public class TransferStatusChangedEventArgs : EventArgs
    {
        public TransferStatusChangedEventArgs(TransferStatus newStatus)
        {
            NewStatus = newStatus;
        }

        /// <summary>
        /// Stato del trasferimento dopo il passaggio di stato
        /// </summary>
        public TransferStatus NewStatus
        {
            get;
            private set;
        }
    }
}
