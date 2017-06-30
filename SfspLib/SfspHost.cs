using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta un host remoto
    /// </summary>
    public class SfspHost
    {
        /// <summary>
        /// Inizializza un nuovo oggetto di tipo Host
        /// </summary>
        /// <param name="scanResponse">Messaggio inviato dall'host in questione come risposta alla richiesta di scansione</param>
        /// <param name="remoteAddress">Indirizzo IP dell'host remoto</param>
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
        /// Effettua la sansione ricorsiva di una directory (o di un file, nel qual caso rileva solo il file specificato)
        /// Elencando tutti gli elementi in essa contenuti.
        /// </summary>
        /// <param name="path">Percorso della directory o del file</param>
        /// <param name="foundObjects">Lista in cui aggiungere gli oggetti trovati</param>
        private void Scan(string path, List<string> foundObjects)
        {
            // Se l'elemento specificato è una directory
            if (Directory.Exists(path))
            {
                // La aggiungo nella lista PRIMA dei suoi figli
                foundObjects.Add(path);

                // Elenco tutti i file in essa contenuti e li aggiungo
                string[] files = Directory.GetFiles(path);
                foreach (string filePath in files)
                    foundObjects.Add(filePath);

                // Elenco tutte le sottodirectory in essa contenute e chiamo su esse ricorsivamente Scan
                string[] subDirs = Directory.GetDirectories(path);
                foreach (string dirPath in subDirs)
                    Scan(dirPath, foundObjects);
            }
            // Se l'elemento è un file mi limito ad aggiungerlo
            else if (File.Exists(path))
                foundObjects.Add(path);
        }

        /// <summary>
        /// Invia un file o una directory
        /// </summary>
        /// <param name="path">Percorso dell'oggetto da inviare</param>
        /// <returns></returns>
        public SfspAsyncUpload Send(string path)
        {
            // Directory contenente l'elemento da inviare (la usiamo per il calcolo dei percorsi relativi)
            String basePath = Path.GetDirectoryName(path);

            // Elenco tutti gli oggetti (file e cartelle) da inviare
            List<string> objects = new List<string>();
            Scan(path, objects);
            // Determino i percorsi relativi degli oggetti da inviare
            List<string> relativePathObjects = objects.Select(s => PathUtils.GetRelativePath(basePath, s)).ToList();

            // La lista non può essere vuota
            if (objects.Count == 0)
                throw new FileNotFoundException();

            SfspAsyncUpload upload = new SfspAsyncUpload(this, basePath, relativePathObjects);

            return upload;
        }

        /// <summary>
        /// Crea una connessione TCP con l'host remoto
        /// </summary>
        /// <returns>L'oggetto TcpClient relativo alla connessione con l'host remoto</returns>
        internal TcpClient CreateConnection()
        {
            IPEndPoint endpoint = new IPEndPoint(this.Address, this.TcpPort);
            TcpClient tcp = new TcpClient();
            tcp.Connect(endpoint);

            return tcp;
        }
    }
}
