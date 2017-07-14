using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Rappresenta il messaggio inviato da un host per cercare altri host sulla rete.
    /// Si tratta dell'unico messaggio inviato mediante UDP e contiene il numero di una porta TCP
    /// temporaneamente aperta dall'host, sulla quale esso si aspetta una risposta
    /// </summary>
    internal class SfspScanRequestMessage : SfspMessage
    {
        private ushort _TcpPort;

        /// <summary>
        /// Costruttore usato per creare un messaggio da inviare
        /// </summary>
        /// <param name="tcpPort">Porta TCP a cui va inviata la risposta</param>
        public SfspScanRequestMessage(ushort tcpPort) : base(SfspMessageTypes.ScanRequest)
        {
            _TcpPort = tcpPort;
        }

        protected override void WriteData()
        {
            WriteShort(_TcpPort);
        }

        protected override void ReadData()
        {
            _TcpPort = ReadShort();
        }

        /// <summary>
        /// Porta TCP a cui va inviata la risposta
        /// </summary>
        public ushort TcpPort
        {
            get
            {
                return _TcpPort;
            }
            set
            {
                _TcpPort = value;
            }
        }
    }
}
