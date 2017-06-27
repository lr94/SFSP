using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFSP.Messaging
{
    internal class SfspRequestMessage : SfspMessage
    {
        private ulong _TotalSize;
        IList<String> _RelativePaths;

        public SfspRequestMessage(ulong totalSize, IList<String> relativePaths) : base(SfspMessageTypes.Request)
        {
            _TotalSize = totalSize;
            _RelativePaths = relativePaths;
        }

        protected override void ReadData()
        {
            _TotalSize = ReadLong();
            _RelativePaths = ReadStringList();
        }

        protected override void WriteData()
        {
            WriteLong(_TotalSize);
            WriteStringList(_RelativePaths);
        }

        public ulong TotalSize
        {
            get
            {
                return _TotalSize;
            }
            set
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
