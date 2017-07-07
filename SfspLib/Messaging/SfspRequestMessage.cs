using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sfsp.Messaging
{
    internal class SfspRequestMessage : SfspMessage
    {
        private ulong _TotalSize;
        IList<String> _RelativePaths;

        public SfspRequestMessage(string hostName, ulong totalSize, IList<String> relativePaths) : base(SfspMessageTypes.Request)
        {
            RemoteHostName = hostName;
            _TotalSize = totalSize;
            _RelativePaths = relativePaths;
        }

        protected override void ReadData()
        {
            RemoteHostName = ReadString();
            _TotalSize = ReadLong();
            _RelativePaths = ReadStringList();
        }

        protected override void WriteData()
        {
            WriteString(RemoteHostName);
            WriteLong(_TotalSize);
            WriteStringList(_RelativePaths);
        }

        public string RemoteHostName
        {
            get;
            protected set;
        }

        public ulong TotalSize
        {
            get
            {
                return _TotalSize;
            }
            protected set
            {
                _TotalSize = value;
            }
        }

        public IList<String> RelativePaths
        {
            get
            {
                return _RelativePaths;
            }
            set
            {
                _RelativePaths = value;
            }
        }
    }
}
