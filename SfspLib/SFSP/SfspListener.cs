using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using SFSP.Messaging;

namespace SFSP
{
    /// <summary>
    /// Consente all'host di risultare visibile sulla rete locale e di ricevere file
    /// </summary>
    public class SfspListener
    {
        private SfspHostConfiguration _Configuration;
        private UdpClient udpClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Configurazione dell'host</param>
        public SfspListener(SfspHostConfiguration configuration)
        {
            _Configuration = configuration;

            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, _Configuration.UdpPort));
            udpClient.JoinMulticastGroup(_Configuration.MulticastAddress, IPAddress.Any);
        }

        public void Start()
        {
            Thread t = new Thread(ServerTask);
            t.Start();
        }

        private void ServerTask()
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
