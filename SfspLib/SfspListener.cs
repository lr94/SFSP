using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Sfsp.Messaging;

namespace Sfsp
{
    /// <summary>
    /// Consente all'host di risultare visibile sulla rete locale e di ricevere file
    /// </summary>
    public class SfspListener
    {
        private List<UdpClient> udpClients;
        private TcpListener tcpListener;

        private object locker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">Configurazione dell'host</param>
        public SfspListener(SfspHostConfiguration configuration)
        {
            Configuration = configuration;

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
                    currentClient.MulticastLoopback = Configuration.AllowLoopback;
                    // ...in modo da essere sicuro di ascoltare per pacchetti multicast su tutte le interfacce
                    currentClient.JoinMulticastGroup(Configuration.MulticastAddress, currentIP);

                    udpClients.Add(currentClient);
                }
                catch (SocketException)
                {
                    // Probabilmente l'interfaccia corrente era già in ascolto o non supportava multicast
                    // mi limito ad ignorare la cosa
                }
            }

            // Se non abbiamo client UDP in ascolto su nessuna interfaccia
            if(udpClients.Count == 0)
                throw new Exception("Could not initialize SfspListener");
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
            // Per ogni UDP client creo un thread che stia in ascolto
            foreach (UdpClient udpClient in udpClients)
            {
                // Ok, qui perdiamo il riferimento all'oggetto Thread ma tanto non ce ne facciamo niente
                // (e il garbage collector non si libererà di thread in esecuzione)
                Thread udpListenerThread = new Thread(() => UdpServerTask(udpClient));
                udpListenerThread.IsBackground = true;
                udpListenerThread.Start();
            }

            // Creo il listener per le connessioni TCP
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, Configuration.TcpPort));
            tcpListener.Start();

            // ...e il relativo thread di ascolto
            Thread tcpListenerThread = new Thread(TcpServerTask);
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();
        }

        private void TcpServerTask()
        {
            while (true)
            {
                // Si è connesso qualcuno
                TcpClient client = tcpListener.AcceptTcpClient();

                // Se siamo invisibili non vogliamo ricevere nulla, ci disconnettiamo
                if (!Configuration.Online)
                {
                    client.Close();
                    continue;
                }

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


        private void UdpServerTask(UdpClient udpClient)
        {
            while (true)
            {
                // Ricevo un pacchetto e ne determino l'origine
                IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] datagram = udpClient.Receive(ref remoteEndpoint);

                // Creo il relativo oggetto SfspMessage
                MemoryStream ms = new MemoryStream(datagram);
                SfspMessage msg = SfspMessage.ReadMessage(ms);

                // Se non vogliamo essere rilevabili ci limitiamo a non rispondere
                if (!Configuration.Online)
                    continue;

                // Se il messaggio era dovuto a una scansione (l'unico consentito su UDP, in effetti)
                if (msg is SfspScanRequestMessage)
                {
                    SfspScanRequestMessage scanRequest = (SfspScanRequestMessage)msg;

                    // Rispondo con il mio nome e la mia porta TCP
                    SfspScanResponseMessage scanResponse = new SfspScanResponseMessage(Configuration.Name, Configuration.TcpPort);
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(remoteEndpoint.Address, scanRequest.TcpPort);
                    NetworkStream stream = tcpClient.GetStream();
                    scanResponse.Write(stream);
                    tcpClient.Close();
                }
                // Niente else: se arriva un altro messaggio (non ScanRequest) in violazione del protocollo mi limito a ignorarlo
            }
        }

        /// <summary>
        /// Configurazione dell'host
        /// </summary>
        public SfspHostConfiguration Configuration
        {
            get;
            private set;
        }

        /// <summary>
        /// Lista degli endpoint UDP su cui il listener è in ascolto
        /// </summary>
        public IReadOnlyCollection<IPEndPoint> UdpLocalEndpoints
        {
            get
            {
                return udpClients.Select(c => c.Client.LocalEndPoint as IPEndPoint).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Endpoint TCP su cui il listener è in ascolto
        /// </summary>
        public IPEndPoint TcpLocalEndpoint
        {
            get
            {
                return tcpListener.LocalEndpoint as IPEndPoint;
            }
        }
    }
}
