using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Linq;
using Sfsp.Messaging;
using Sfsp.TcpUtils;

namespace Sfsp
{
    /// <summary>
    /// Rappresenta il trasferimento verso un altro host di un insieme di oggetti (file e cartelle)
    /// </summary>
    public class SfspAsyncUpload : SfspAsyncTransfer
    {
        private string basePath;
        private SfspHost remoteHost;
        private SfspHostConfiguration configuration;

        private Thread uploadThread;

        private bool started = false;

        /// <summary>
        /// Inizializza un nuovo trasferimento (in stato "New").
        /// </summary>
        /// <param name="basePath">Directory "radice" contenente gli oggetti da trasferire</param>
        /// <param name="relativePaths">Elenco di tutti gli oggetti da trasferire, espressi come percorsi relativi rispetto alla radice</param>
        internal SfspAsyncUpload(SfspHost remoteHost, string basePath, List<string> relativePaths, SfspHostConfiguration thisHostConfig)
        {
            this.remoteHost = remoteHost;
            this.relativePaths = relativePaths;
            this.basePath = basePath;
            this.configuration = thisHostConfig;

            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException("Cannot find base directory " + basePath + ".");

            if (relativePaths.Count == 0)
                throw new ArgumentException("Cannot send an empty file set", "relativePaths");

            this.Status = TransferStatus.New;
        }

        
        /// <summary>
        /// Avvia l'upload di tutti gli oggetti selezionati per il trasferimento
        /// </summary>
        public void Start()
        {
            // Verifichiamo che l'oggetto non sia già stato utilizzato per un trasferimento
            // uso lock perché altrimenti in linea teorica un altro thread potrebbe chiamare
            // Start() mentre questo si trova tra if(started) e started = true.
            lock(locker)
            {
                if (started)
                    throw new InvalidOperationException("The transfer has already been started");

                started = true;
            }

            uploadThread = new Thread(UploadTask);
            uploadThread.IsBackground = true;
            uploadThread.Start();
        }

        private void UploadTask()
        {
            TcpClient client = null;
            SfspNetworkStream stream = null;

            try
            {
                // Mi collego all'host remoto
                client = remoteHost.CreateConnection();
                stream = new SfspNetworkStream(client.Client);

                // Scorro tutti gli oggetti da inviare
                long totalSize = 0;
                foreach (string currentObject in relativePaths)
                {
                    // Ne ottengo il percorso completo per poi determinarne la dimensione, che aggiungo a totalSize
                    String fullpath = Path.Combine(basePath, currentObject);
                    if (File.Exists(fullpath))
                    {
                        FileInfo fi = new FileInfo(fullpath);
                        totalSize += fi.Length;
                    }
                }
                TotalSize = totalSize;

                // Preparo il messaggio di richiesta da inviare
                List<string> relativeSFSPPaths = relativePaths.Select(s => PathUtils.ConvertOSPathToSFSP(s)).ToList();
                SfspRequestMessage request = new SfspRequestMessage(configuration.Name, (ulong)totalSize, relativeSFSPPaths);

                // Invio il messaggio
                request.Write(stream);
                SetStatus(TransferStatus.Pending);

                // Leggo il messaggio di risposta
                SfspMessage msg = SfspMessage.ReadMessage(stream);

                // Ci aspettiamo una risposta di tipo Confirm!
                if (!(msg is SfspConfirmMessage))
                    throw new ProtocolViolationException("Expected Confirm message, got " + msg.MessageType.ToString());

                SfspConfirmMessage confirm = (SfspConfirmMessage)msg;
                // Se l'invio è stato rifiutato...
                if (confirm.Status == SfspConfirmMessage.FileStatus.Error)
                    this.FailureException = new TransferAbortException(TransferAbortException.AbortType.RemoteAbort);

                // Se arriviamo qui l'invio è stato accettato!
                SetStatus(TransferStatus.InProgress);

                // Inizio il trasferimento vero e proprio
                foreach (String objectRelativePath in relativePaths)
                {
                    // Percorso completo sul sistema locale
                    string fullPath = Path.Combine(basePath, objectRelativePath);

                    // Se è una cartella...
                    if (Directory.Exists(fullPath))
                    {
                        // ...inviamo un comando di creazione della cartella
                        SfspCreateDirectoryMessage createDirMessage = new SfspCreateDirectoryMessage(PathUtils.ConvertOSPathToSFSP(objectRelativePath));
                        createDirMessage.Write(stream);

                        // Attendo conferma
                        msg = SfspMessage.ReadMessage(stream);
                        if (!(msg is SfspConfirmMessage))
                            throw new ProtocolViolationException("Expected Confirm message, got " + msg.MessageType.ToString());
                        confirm = (SfspConfirmMessage)msg;
                        if (confirm.Status != SfspConfirmMessage.FileStatus.Ok)
                            throw new Exception("Could not create directory " + objectRelativePath + " on the remote host.");
                    }
                    // Se è un file...
                    else if (File.Exists(fullPath))
                    {
                        // Provo massimo 3 volte ad inviarlo
                        bool done = false;
                        int attempts = 3;
                        do
                        {
                            done |= UploadFile(stream, fullPath, objectRelativePath);
                            attempts--;
                        } while (!done && attempts > 0);
                        if (!done)
                        {
                            throw new Exception("Too many errors");
                        }
                    }
                    else
                        throw new FileNotFoundException("File or directory not found", fullPath);
                }

                // Abbiamo finito
                ForceProgressUpdate();
                SetStatus(TransferStatus.Completed);
            }
            catch(Exception ex)
            {
                this.FailureException = ex;
                SetStatus(TransferStatus.Failed);
            }
            finally
            {
                if (client != null)
                    client.Close();
                if (stream != null)
                    stream.Close();
            }
        }

        private bool UploadFile(NetworkStream stream, string fullPath, string relativePath)
        {
            // Determino la dimensione del file
            FileInfo fInfo = new FileInfo(fullPath);
            long fSize = fInfo.Length;
            // Invio il comando di creazione del file
            SfspCreateFileMessage createFileMessage = new SfspCreateFileMessage((ulong)fSize, PathUtils.ConvertOSPathToSFSP(relativePath));
            createFileMessage.Write(stream);

            // Apro in lettura il file
            FileStream fStream = File.OpenRead(fullPath);

            // Preparazione per il calcolo del checksum
            SHA256 sha256 = SHA256.Create();
            sha256.Initialize();

            // Invio i dati
            byte[] buffer = new byte[BUFFER_SIZE];
            long fSent = 0;
            try
            {
                while (fSent < fSize)
                {
                    if (Aborting)
                        throw new TransferAbortException(TransferAbortException.AbortType.LocalAbort);

                    int bufSize = (fSize - fSent < BUFFER_SIZE) ? (int)(fSize - fSent) : BUFFER_SIZE;
                    // Leggo dal file
                    bufSize = fStream.Read(buffer, 0, bufSize);
                    // Invio i dati
                    stream.Write(buffer, 0, bufSize);
                    // Calcolo del checksum
                    sha256.TransformBlock(buffer, 0, bufSize, buffer, 0);
                    // Aggiorno i contatori
                    fSent += bufSize;
                    progress += bufSize;

                    // Eventuale aggiornamento dell'avanzamento dell'operazione
                    ProgressUpdateIfNeeded();
                }
            }
            finally
            {
                fStream.Close();
            }

            // Invio checksum
            sha256.TransformFinalBlock(buffer, 0, 0);
            byte[] hash = sha256.Hash;
            SfspChecksumMessage checksumMsg = new SfspChecksumMessage(hash);
            checksumMsg.Write(stream);

            // Aspetto una risposta
            SfspMessage receivedMsg = SfspMessage.ReadMessage(stream);
            if (!(receivedMsg is SfspConfirmMessage))
                throw new ProtocolViolationException("Unexpected Sfsp message");
            SfspConfirmMessage confirmMsg = (SfspConfirmMessage)receivedMsg;

            // Il trasferimento è fallito, faccio retrocedere il contatore e restituisco false
            if(confirmMsg.Status != SfspConfirmMessage.FileStatus.Ok)
            {
                progress -= fSize;
                // Aggiornamento dell'avanzamento dell'operazione
                ForceProgressUpdate();

                return false;
            }

            // Tutto a posto
            return true;
        }

        /// <summary>
        /// >Directory "radice" contenente gli oggetti da trasferire
        /// </summary>
        public string BaseDirectory
        {
            get
            {
                return basePath;
            }
        }
    }
}
