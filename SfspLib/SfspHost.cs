using System;
using System.Net;

namespace Sfsp
{
    public class SfspHost
    {
        internal SfspHost(Messaging.SfspScanResponseMessage scanResponse, IPAddress remoteAddress)
        {
            Name = scanResponse.Name;
            TcpPort = scanResponse.TcpPort;
            Address = remoteAddress;
        }

        /// <summary>
        /// Nome dell'host
        /// </summary>
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Porta TCP su cui è in ascolto l'host per la ricezione dei dati
        /// </summary>
        public ushort TcpPort
        {
            get;
            set;
        }

        /// <summary>
        /// Indirizzo dell'host
        /// </summary>
        public IPAddress Address
        {
            get;
            set;
        }


        /// <summary>
        /// Invia un insieme di file o directory
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public SfspAsyncUpload Send(string[] paths)
        {
            return null;
        }
    }
}
