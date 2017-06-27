using System;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Rappresenta il messaggio inviato da un host per cercare altri host sulla rete.
    /// </summary>
    internal class SfspScanResponseMessage : SfspMessage
    {
        /// <summary>
        /// Costruttore usato per creare un messaggio da inviare
        /// </summary>
        /// <param name="tcpPort">Porta TCP a cui va inviata la risposta</param>
        public SfspScanResponseMessage(string name, ushort tcpPort) : base(SfspMessageTypes.ScanResponse)
        {
            Name = name;
            TcpPort = tcpPort;
        }

        protected override void WriteData()
        {
            WriteString(Name);
            WriteShort(TcpPort);
        }

        protected override void ReadData()
        {
            Name = ReadString();
            TcpPort = ReadShort();
        }

        /// <summary>
        /// Nome dell'host
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Porta TCP su cui l'host è in ascolto
        /// </summary>
        public ushort TcpPort
        {
            get;
            set;
        }
    }
}
