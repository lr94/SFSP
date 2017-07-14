using System;
using System.Net;

namespace Sfsp
{

    /// <summary>
    /// Rappresenta la configurazione di un host
    /// </summary>
    public class SfspHostConfiguration
    {
        private object locker = new object();

        public SfspHostConfiguration(string name)
        {
            Name = name;
            TcpPort = 6000;
            UdpPort = 5999;
            MulticastAddress = IPAddress.Parse("239.0.0.1");
            AllowLoopback = true;
        }

        private string _Name;
        /// <summary>
        /// Nome dell'host. Questa proprietà può cambiare quando il server è già avviato ed è thread safe.
        /// </summary>
        public string Name
        {
            get
            {
                string value;
                lock (locker)
                {
                    value = _Name;
                }
                return value;
            }
            set
            {
                lock (locker)
                {
                    _Name = value;
                }
            }
        }

        private bool _Online = true;
        /// <summary>
        /// Specifica se l'host è in modalità Online e se deve essere quindi visibile agli altri host sulla rete
        /// e poter ricevere dati. Se impostato su false il Listener rimane in ascolto sulla rete ma non risponde.
        /// Questa proprietà può cambiare quando il server è già avviato ed è thread safe.
        /// </summary>
        public bool Online
        {
            get
            {
                bool to_ret;
                lock (locker)
                {
                    to_ret = _Online;
                }
                return to_ret;
            }
            set
            {
                lock (locker)
                {
                    _Online = value;
                }
            }
        }

        /// <summary>
        /// Porta TCP su cui l'host deve stare in ascolto per la ricezione di file.
        /// Modificare questa proprietà a server già avviato è inutile e non produce alcun effetto.
        /// </summary>
        public ushort TcpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Porta UDP su cui l'host deve stare in ascolto per eventuali scansioni.
        /// Modificare questa proprietà a server già avviato è inutile e non produce alcun effetto.
        /// </summary>
        public ushort UdpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Indirizzo IP multicast per la scansione mediante UDP.
        /// Modificare questa proprietà a server già avviato è inutile e non produce alcun effetto.
        /// </summary>
        public IPAddress MulticastAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Indica se l'host può inviarsi dei file da solo
        /// </summary>
        public bool AllowLoopback
        {
            get;
            set;
        }
    }
}
