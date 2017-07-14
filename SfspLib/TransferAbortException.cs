using System;

namespace Sfsp
{
    /// <summary>
    /// Eccezione sollevata quando un trasferimento viene annullato dall'host locale o remoto.
    /// </summary>
    public class TransferAbortException : Exception
    {
        public enum AbortType
        {
            /// <summary>
            /// Il trasferimento è stato annullato dall'host locale
            /// </summary>
            LocalAbort,
            /// <summary>
            /// Il trasferimento è stato annullato dall'host remoto
            /// </summary>
            RemoteAbort
        }

        /// <summary>
        /// Specifica chi ha annullato il trasferimento (host locale o host remoto)
        /// </summary>
        public AbortType Type
        {
            get;
            private set;
        }

        public override string Message
        {
            get
            {
                switch(Type)
                {
                    case AbortType.LocalAbort:
                        return "Transfer aborted by the local user";
                    case AbortType.RemoteAbort:
                        return "Transfer aborted by the remote host";
                }
                return base.Message;
            }
        }

        internal TransferAbortException(AbortType type)
        {
            Type = type;
        }
    }
}
