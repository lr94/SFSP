using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Sfsp.Messaging;

namespace Sfsp
{
    /// <summary>
    /// Consente all'host di risultare visibile sulla rete locale e di ricevere file
    /// </summary>
    public class SfspListener
    {
        private SfspHostConfiguration _Configuration;
        private List<UdpClient> udpClients;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Configurazione dell'host</param>
        public SfspListener(SfspHostConfiguration configuration)
        {
            _Configuration = configuration;

            // Determino gli indirizzi IP di tutte le interfacce di rete
            string strHostName = System.Net.Dns.GetHostName();
            IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
            IPAddress[] addresses = ipEntry.AddressList;

            // Per ciascuno di essi creo un client UDP...
            udpClients = new List<UdpClient>();
            foreach (IPAddress currentIP in addresses)
            {
                try
                {
                    UdpClient currentClient = new UdpClient(new IPEndPoint(currentIP, _Configuration.UdpPort));
                    currentClient.MulticastLoopback = true;
                    // ...in modo da essere sicuro di ascoltare per pacchetti multicast su tutte le interfacce
                    currentClient.JoinMulticastGroup(_Configuration.MulticastAddress, currentIP);

                    udpClients.Add(currentClient);
                }
                catch(SocketException)
                {
                    // Probabilmente l'interfaccia corrente era già in ascolto o non supportava multicast
                    // mi limito ad ignorare la cosa
                }
            }
        }

        /// <summary>
        /// Mette l'host locale in ascolto
        /// </summary>
        public void Start()
        {
            foreach(UdpClient udpClient in udpClients)
            {
                Thread t = new Thread(() => ServerTask(udpClient));
                t.Start();
            }
        }

        private void ServerTask(UdpClient udpClient)
        {
            while(true)
            {
                IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] datagram = udpClient.Receive(ref remoteEndpoint);

                MemoryStream ms = new MemoryStream(datagram);
                SfspMessage msg = SfspMessage.ReadMessage(ms);

                if(msg is SfspScanRequestMessage)
                {
                    SfspScanRequestMessage scanRequest = (SfspScanRequestMessage)msg;

                    SfspScanResponseMessage scanResponse = new SfspScanResponseMessage(_Configuration.Name, _Configuration.TcpPort);
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(remoteEndpoint.Address, scanRequest.TcpPort);
                    NetworkStream stream = tcpClient.GetStream();
                    scanResponse.Write(stream);
                    tcpClient.Close();
                }
            }
        }

        /// <summary>
        /// Configurazione dell'host
        /// </summary>
        public SfspHostConfiguration Configuration
        {
            get
            {
                return _Configuration;
            }
        }
    }
}
