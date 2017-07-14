using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Rappresenta un messaggio di tipo request, attraverso il quale un host comunica ad un altro
    /// che vuole inviargli dei dati.
    /// Il messaggio contiene il nome dell'host che vuole inviare i dati, la loro dimensione totale
    /// (contando solo i file) e la lista di tutti i percorsi relativi dei file e delle cartelle
    /// </summary>
    internal class SfspRequestMessage : SfspMessage
    {
        private ulong _TotalSize;
        List<String> _RelativePaths;

        public SfspRequestMessage(string hostName, ulong totalSize, List<String> relativePaths) : base(SfspMessageTypes.Request)
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

        public List<String> RelativePaths
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
