using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFSP.Messaging
{
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
