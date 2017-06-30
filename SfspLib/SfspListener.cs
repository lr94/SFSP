using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using Sfsp.Messaging;

namespace Sfsp
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

        /// <summary>
        /// Mette l'host locale in ascolto
        /// </summary>
        public void Start()
        {
            Thread udpListenerThread = new Thread(UdpServerTask);
            udpListenerThread.Start();

            Thread tcpListenerThread = new Thread(TcpServerTask);
            tcpListenerThread.Start();
        }

        private void TcpServerTask()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, _Configuration.TcpPort));
            listener.Start();

            while(true)
            {
                // Si è connesso qualcuno
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                // Leggo il messaggio inviato (ignorando eventuali errori)
                SfspMessage receivedMsg;
                try
                {
                    receivedMsg = SfspMessage.ReadMessage(stream);
                } catch (SfspInvalidMessageException)
                {
                    // Se il messaggio non era valido ignoriamo direttamente questa richiesta di connessione
                    continue;
                }

                // Ignoro eventuali messaggi di tipo non atteso
                if (receivedMsg is SfspRequestMessage)
                {
                    SfspRequestMessage request = (SfspRequestMessage)receivedMsg;

                    System.Diagnostics.Debug.WriteLine("Ricevuta richiesta, bytes: " + request.TotalSize.ToString());
                }
            }
        }

        private void UdpServerTask()
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
