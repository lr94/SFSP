using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Questa classe rappresenta un messaggio Sfsp Confirm. Tale tipo di messaggio ha un duplice scopo:
    /// - Inviato dal ricevente per accettare (Status = Ok) o rifiutare (Status = Error) un invio
    /// - Inviato dal ricevente al termine della ricezione di ogni oggetto, per indicare se il file è stato ricevuto
    ///   correttamente o ci sono stati problemi
    /// </summary>
    internal class SfspConfirmMessage : SfspMessage
    {
        public enum FileStatus : byte
        {
            Ok = 0,
            Error = 1
        }

        public SfspConfirmMessage(FileStatus status) : base(SfspMessageTypes.Confirm)
        {
            Status = status;
        }

        protected override void ReadData()
        {
            Status = (FileStatus)ReadByte();
        }

        protected override void WriteData()
        {
            WriteByte((byte)Status);
        }

        public FileStatus Status
        {
            get;
            set;
        }
    }
}
