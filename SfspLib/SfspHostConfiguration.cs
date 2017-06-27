using System;
using System.Net;

namespace Sfsp
{

    /// <summary>
    /// Rappresenta la configurazione di un host
    /// </summary>
    public class SfspHostConfiguration
    {
        public SfspHostConfiguration(string name)
        {
            Name = name;
            TcpPort = 6000;
            UdpPort = 5999;
            MulticastAddress = IPAddress.Parse("239.0.0.1");
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
        /// Porta TCP su cui l'host deve stare in ascolto per la ricezione di file
        /// </summary>
        public ushort TcpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Porta UDP su cui l'host deve stare in ascolto per eventuali scansioni
        /// </summary>
        public ushort UdpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Indirizzo IP multicast per la scansione mediante UDP
        /// </summary>
        public IPAddress MulticastAddress
        {
            get;
            set;
        }
    }
}
