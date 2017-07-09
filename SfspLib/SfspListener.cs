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
                    UdpClient currentClient = new UdpClient(new IPEndPoint(currentIP, Configuration.UdpPort));
                    currentClient.MulticastLoopback = true;
                    // ...in modo da essere sicuro di ascoltare per pacchetti multicast su tutte le interfacce
                    currentClient.JoinMulticastGroup(Configuration.MulticastAddress, currentIP);

                    udpClients.Add(currentClient);
                }
                catch(SocketException)
                {
                    // Probabilmente l'interfaccia corrente era già in ascolto o non supportava multicast
                    // mi limito ad ignorare la cosa
                }
            }
        }

        public event EventHandler<TransferRequestEventArgs> TransferRequest;
        protected void OnTransferRequest(SfspAsyncDownload download)
        {
            if (TransferRequest != null)
                TransferRequest(this, new TransferRequestEventArgs(download));
        }

        /// <summary>
        /// Mette l'host locale in ascolto
        /// </summary>
        public void Start()
        {
            foreach (UdpClient udpClient in udpClients)
            {
                Thread udpListenerThread = new Thread(() => ServerTask(udpClient));
                udpListenerThread.Start();
            }

            Thread tcpListenerThread = new Thread(TcpServerTask);
            tcpListenerThread.Start();
        }

        private void TcpServerTask()
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, Configuration.TcpPort));
            listener.Start();

            while (true)
            {
                // Si è connesso qualcuno
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                // Leggo il messaggio inviato (ignorando eventuali errori)
                SfspMessage receivedMsg;
                try
                {
                    receivedMsg = SfspMessage.ReadMessage(stream);
                }
                catch (SfspInvalidMessageException)
                {
                    // Se il messaggio non era valido ignoriamo direttamente questa richiesta di connessione
                    continue;
                }

                // Ignoro eventuali messaggi di tipo non atteso
                if (receivedMsg is SfspRequestMessage)
                {
                    SfspRequestMessage request = (SfspRequestMessage)receivedMsg;

                    // Genero l'oggetto per il download e sollevo l'evento
                    OnTransferRequest(new SfspAsyncDownload(request, client));
                }
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

                    SfspScanResponseMessage scanResponse = new SfspScanResponseMessage(Configuration.Name, Configuration.TcpPort);
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
            get;
        }
    }
}
