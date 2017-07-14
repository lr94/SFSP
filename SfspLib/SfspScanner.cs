using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Sfsp.Messaging;

namespace Sfsp
{
    /// <summary>
    /// Consente di cercare sulla rete locale host a cui inviare i file mediante il protocollo Sfsp
    /// </summary>
    public class SfspScanner
    {
        private SfspHostConfiguration _Configuration;
        private List<UdpClient> udpClients;
        private TcpListener tcpListener;

        private List<IPAddress> foundIPs = new List<IPAddress>();

        /// <summary>
        /// Evento sollevato ogni volta che viene trovato un nuovo host.
        /// Notare che ogni host viene trovato da un thread diverso, pertanto l'event handler
        /// deve occuparsi della sincronizzazione.
        /// </summary>
        public event EventHandler<SfspHostFoundEventArgs> HostFound;

        /// <summary>
        /// Evento sollevato al termine della scansione (ovvero allo scadere del timeout specificato)
        /// </summary>
        public event EventHandler<EventArgs> ScanComplete;

        /// <summary>
        /// Inizializza un nuovo oggetto scanner
        /// </summary>
        /// <param name="configuration">Configurazione dell'host che effettua la ricerca</param>
        public SfspScanner(SfspHostConfiguration configuration)
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
                UdpClient currentClient = new UdpClient();
                currentClient.MulticastLoopback = _Configuration.AllowLoopback;
                // ...in modo da essere sicuro di inviare i pacchetti multicast su tutte le interfacce.
                // Questo si è reso necessario dopo che mi sono accorto che su Windows i pacchetti UDP
                // venivano indirizzati tutti sull'interfaccia di VirtualBox (192.168.1.51?) invece che sulla
                // rete locale. Lo svantaggio di questa tecnica è che a uno stesso host potranno arrivare più
                // copie di un messaggio ScanRequest (dato che in addresses[] ci sono gli IPv4 e IPv6 delle stesse
                // interfacce), ma del resto questo è comunque da mettere in conto visto che stiamo usando
                // UDP e non TCP
                currentClient.JoinMulticastGroup(_Configuration.MulticastAddress, currentIP);
                currentClient.Connect(new IPEndPoint(_Configuration.MulticastAddress, _Configuration.UdpPort));

                udpClients.Add(currentClient);
            }

            // Inizializzo server TCP su porta libera random
            tcpListener = new TcpListener(IPAddress.Any, 0);
        }

        /// <summary>
        /// Avvia la scansione asincrona
        /// </summary>
        /// <param name="timeout">Intervallo di tempo che deve passare dall'ultimo rilevamento di un host prima di decretare la fine della scansione</param>
        public void StartScan(TimeSpan timeout)
        {
            Thread t = new Thread(ScannerTask);
            t.Start(timeout);
        }

        /// <summary>
        /// Avvia la scansione asincrona
        /// </summary>
        /// <param name="timeout">Millisecondi che devono passare dall'ultimo rilevamento di un host prima di decretare la fine della scansione</param>
        public void StartScan(int timeoutMilliseconds)
        {
            StartScan(TimeSpan.FromMilliseconds((double)timeoutMilliseconds));
        }

        private void ScannerTask(Object obj)
        {
            TimeSpan timeout;
            if (obj is TimeSpan)
                timeout = (TimeSpan)obj;
            else
                throw new ArgumentException();

            try
            {
                // Azzero la lista degli IP trovati
                foundIPs.Clear();

                // Avvio il server TCP
                tcpListener.Start();

                // Preparo il messaggio ScanRequest
                IPEndPoint localEndPoint = (IPEndPoint)tcpListener.LocalEndpoint;
                ushort localTcpPort = (ushort)localEndPoint.Port;
                SfspScanRequestMessage req = new SfspScanRequestMessage(localTcpPort);
                // Invio il messaggio
                byte[] req_bytes = req.GetBytes();
                foreach (UdpClient currentClient in udpClients)
                    currentClient.Send(req_bytes, req_bytes.Length);

                // Ora aspetto che gli altri host rispondano
                while (true)
                {
                    // Attendo la connessione di un client col timeout specificato
                    IAsyncResult result = tcpListener.BeginAcceptTcpClient(null, null);
                    if (!result.AsyncWaitHandle.WaitOne(timeout))
                    {
                        // Timeout
                        if (ScanComplete != null)
                            ScanComplete(this, new EventArgs());
                        break;
                    }
                    // Un client si è connesso, ottengo lo stream relativo alla connessione
                    TcpClient client = tcpListener.EndAcceptTcpClient(result);

                    NetworkStream stream = client.GetStream();

                    // Nessuno garantisce che chi ha inviato rispetti il protocollo
                    try
                    {
                        // Leggo il messaggio ricevuto
                        SfspMessage msg = SfspMessage.ReadMessage(stream);

                        // Ci interessa solo se è uno ScanResponse (e non dovrebbe essere nient'altro; nel caso ignoro)
                        if (msg is SfspScanResponseMessage)
                        {
                            // Creo un nuovo oggetto Host
                            SfspScanResponseMessage scanResponse = (SfspScanResponseMessage)msg;
                            IPAddress remoteAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                            SfspHost host = new SfspHost(scanResponse, remoteAddress);

                            // Se l'IP è nuovo
                            if (!foundIPs.Contains(remoteAddress))
                            {
                                if (HostFound != null)
                                    HostFound(this, new SfspHostFoundEventArgs(host));
                                foundIPs.Add(remoteAddress);
                            }

                        }
                    }
                    catch (SfspInvalidMessageException) // Se il messaggio non era valido mi limito ad ignorarlo
                    {
                        continue;
                    }
                }
            }
            catch (Exception e) // In caso di eccezione
            {
                // Rilancio l'eccezione avendo però cura di fermare il server
                // Non uso finally {...} perché rilanciando l'eccezione non sarebbe eseguito
                tcpListener.Stop();
                throw e;
            }

            tcpListener.Stop();
        }

        /// <summary>
        /// Configurazione dell'host che effettua la ricerca
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
