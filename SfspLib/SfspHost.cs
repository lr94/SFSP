using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

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
            throw new NotImplementedException();
        }
    }
}
