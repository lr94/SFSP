using System;

namespace Sfsp
{
    public class TransferAbortException : Exception
    {
        public enum AbortType
        {
            LocalAbort,
            RemoteAbort
        }

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
