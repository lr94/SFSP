using System;
using System.IO;
using System.Collections.Generic;

namespace SFSP.Messaging
{
    /// <summary>
    /// Rappresenta un generico messaggio SFSP
    /// </summary>
    internal abstract class SfspMessage
    {
        /// <summary>
        /// Tipi di messaggi SFSP
        /// </summary>
        public enum SfspMessageTypes : ushort
        {
            ScanRequest = 1,
            ScanResponse = 2,
            Request = 3,
            Response = 4,
            CreateDirectory = 5,
            CreateFile = 6,
            Checksum = 7,
            Confirm = 8
        }

        private const byte MY_PROTOCOL_VERSION_MAJOR = 0;
        private const byte MY_PROTOCOL_VERSION_MINOR = 1;

        protected Stream dataStream;
        protected SfspMessageTypes _MessageType;
        protected uint dataLength;

        private int _protocolVersionMajor = MY_PROTOCOL_VERSION_MAJOR;
        private int _protocolVersionMinor = MY_PROTOCOL_VERSION_MINOR;

        protected SfspMessage(SfspMessageTypes messageType)
        {
            _MessageType = messageType;
        }

        /// <summary>
        /// Legge un messaggio SFSP e crea un'istanza di una classe derivata adatta per rappresentarlo.
        /// </summary>
        /// <param name="sourceStream">Stream da cui leggere il messaggio</param>
        /// <returns>Oggetto rappresentante il messaggio ricevuto.</returns>
        public static SfspMessage ReadMessage(Stream sourceStream)
        {
            Stream dataStream = sourceStream;

            // Controllo che il messaggio inizi con "SFSP"
            byte[] magicBytes = new byte[4];
            if (dataStream.Read(magicBytes, 0, 4) != 4)
                throw new SfspInvalidMessageException();
            if (magicBytes[0] != 0x53 || magicBytes[1] != 0x46 || magicBytes[2] != 0x53 || magicBytes[3] != 0x50)
                throw new SfspInvalidMessageException();

            // Controllo che la versione del protocollo sia supportata
            int protocolVersionMajor = dataStream.ReadByte();
            int protocolVersionMinor = dataStream.ReadByte();
            if(protocolVersionMajor == -1 || protocolVersionMinor == -1)
                throw new SfspInvalidMessageException();
            if (protocolVersionMajor > MY_PROTOCOL_VERSION_MAJOR || (protocolVersionMajor == MY_PROTOCOL_VERSION_MAJOR && protocolVersionMinor > MY_PROTOCOL_VERSION_MINOR))
                throw new UnsupportedSfspVersionException(protocolVersionMajor, protocolVersionMinor);

            // Leggo il tipo di messaggio
            byte[] messageTypeBytes = new byte[2];
            if(dataStream.Read(messageTypeBytes,0,2) != 2)
                throw new SfspInvalidMessageException();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(messageTypeBytes);
            SfspMessageTypes messageType = (SfspMessageTypes)BitConverter.ToUInt16(messageTypeBytes, 0);

            // Leggo la lunghezza dei dati
            byte[] dataLengthBytes = new byte[4];
            if (dataStream.Read(dataLengthBytes, 0, 4) != 4)
                throw new SfspInvalidMessageException();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(dataLengthBytes);
            uint dataLength = BitConverter.ToUInt32(dataLengthBytes, 0);

            // Creo un'istanza della classe opportuna
            SfspMessage msg;
            switch(messageType)
            {
                case SfspMessageTypes.ScanRequest:
                    msg = new SfspScanRequestMessage(0);
                    break;
                case SfspMessageTypes.ScanResponse:
                    msg = new SfspScanResponseMessage("", 0);
                    break;
                case SfspMessageTypes.CreateDirectory:
                    msg = new SfspCreateDirectoryMessage("");
                    break;
                case SfspMessageTypes.CreateFile:
                    msg = new SfspCreateFileMessage(0, "");
                    break;
                case SfspMessageTypes.Request:
                    msg = new SfspRequestMessage(0, null);
                    break;
                case SfspMessageTypes.Confirm:
                    msg = new SfspConfirmMessage(SfspConfirmMessage.FileStatus.Ok);
                    break;
                default:
                    throw new SfspInvalidMessageException();
            }

            msg.dataLength = dataLength;
            msg.dataStream = dataStream;
            msg._protocolVersionMajor = protocolVersionMajor;
            msg._protocolVersionMinor = protocolVersionMinor;

            msg.ReadData();

            msg.dataStream = null;

            return msg;
        }

        #region "Codice per la lettura e la scrittura di dati"
        /// <summary>
        /// Legge un singolo byte dal messaggio SFSP
        /// </summary>
        /// <returns>Il byte letto</returns>
        protected byte ReadByte()
        {
            int res = dataStream.ReadByte();

            // Se non è stato possibile leggere il byte che ci aspettavamo lancio un'eccezione
            if (res == -1)
                throw new SfspInvalidMessageException();

            return (byte)res;
        }

        /// <summary>
        /// Scrive un byte nel messaggio SFSP in preparazione
        /// </summary>
        /// <param name="value">Byte da scrivere</param>
        protected void WriteByte(byte value)
        {
            dataStream.WriteByte(value);
        }

        /// <summary>
        /// Legge un intero senza segno a 16 bit dal messaggio SFSP
        /// </summary>
        /// <returns>Il valore letto</returns>
        protected ushort ReadShort()
        {
            byte[] buffer = new byte[2];

            // Provo a leggere 2 byte, in caso di fallimento lancio un'eccezione
            if (dataStream.Read(buffer, 0, 2) != 2)
                throw new SfspInvalidMessageException();

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte (che vengono letti come big endian)
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            // Converto i byte letti nel valore da restituire
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Scrive un intero senza segno a 16 bit nel messaggio SFSP in preparazione
        /// </summary>
        /// <param name="value">Valore da scrivere</param>
        protected void WriteShort(ushort value)
        {
            // Ottengo la rappresentazione su 2 byte del valore
            byte[] buffer = BitConverter.GetBytes(value);

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte per avere l'ordine big endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            dataStream.Write(buffer, 0, 2);
        }

        /// <summary>
        /// Legge un intero senza segno a 32 bit dal messaggio SFSP
        /// </summary>
        /// <returns>Il valore letto</returns>
        protected uint ReadInteger()
        {
            byte[] buffer = new byte[4];

            // Provo a leggere 4 byte, in caso di fallimento lancio un'eccezione
            if (dataStream.Read(buffer, 0, 4) != 4)
                throw new SfspInvalidMessageException();

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte (che vengono letti come big endian)
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            // Converto i byte letti nel valore da restituire
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Scrive un intero senza segno a 32 bit nel messaggio SFSP in preparazione
        /// </summary>
        /// <param name="value">Valore da scrivere</param>
        protected void WriteInteger(uint value)
        {
            // Ottengo la rappresentazione su 4 byte del valore
            byte[] buffer = BitConverter.GetBytes(value);

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte per avere l'ordine big endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            dataStream.Write(buffer, 0, 4);
        }

        /// <summary>
        /// Legge un intero senza segno a 64 bit dal messaggio SFSP
        /// </summary>
        /// <returns>Il valore letto</returns>
        protected ulong ReadLong()
        {
            byte[] buffer = new byte[8];

            // Provo a leggere 4 byte, in caso di fallimento lancio un'eccezione
            if (dataStream.Read(buffer, 0, 8) != 8)
                throw new SfspInvalidMessageException();

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte (che vengono letti come big endian)
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            // Converto i byte letti nel valore da restituire
            return BitConverter.ToUInt64(buffer, 0);
        }

        /// <summary>
        /// Scrive un intero senza segno a 64 bit nel messaggio SFSP in preparazione
        /// </summary>
        /// <param name="value">Valore da scrivere</param>
        protected void WriteLong(ulong value)
        {
            // Ottengo la rappresentazione su 8 byte del valore
            byte[] buffer = BitConverter.GetBytes(value);

            // Se la macchina su cui lavoriamo è little endian invertiamo i byte per avere l'ordine big endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            dataStream.Write(buffer, 0, 8);
        }

        /// <summary>
        /// Legge una stringa UTF8 (max 65535 bytes) dal messaggio SFSP
        /// </summary>
        /// <returns>La stringa letta</returns>
        protected string ReadString()
        {
            // Leggo il numero di byte che compongono la stringa
            ushort nbytes = ReadShort();
            // Preparo un buffer adeguatamente dimensionato
            byte[] buffer = new byte[nbytes];

            // Provo a leggere il numero di byte che ci aspettiamo
            if (dataStream.Read(buffer, 0, nbytes) != nbytes)
                throw new SfspInvalidMessageException();

            // Decodifico i byte letti
            return System.Text.Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Scrive una stringa (codificata in max 65535 bytes UTF8) nel messaggio SFSP
        /// </summary>
        /// <param name="value">Stringa da scrivere</param>
        protected void WriteString(string value)
        {
            // Codifica la stringa in UTF8
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
            int nbytes = buffer.Length;

            // Se supera la lunghezza massima lancio un'eccezione
            if (nbytes > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("value");

            // Scrivo il numero di byte
            WriteShort((ushort)nbytes);
            // Scrivo la stringa
            dataStream.Write(buffer, 0, nbytes);
        }

        /// <summary>
        /// Legge dei dati binari dal messaggio
        /// </summary>
        /// <returns>Vettore di byte letti dal messaggio</returns>
        protected byte[] ReadBlob()
        {
            // Leggo il numero di byte
            ushort nbytes = ReadShort();
            // Preparo un buffer adeguatamente dimensionato
            byte[] buffer = new byte[nbytes];

            // Provo a leggere il numero di byte che ci aspettiamo
            if (dataStream.Read(buffer, 0, nbytes) != nbytes)
                throw new SfspInvalidMessageException();

            return buffer;
        }

        /// <summary>
        /// Scrive dati binari nel messaggio
        /// </summary>
        /// <param name="buffer">Vettore contenente i byte da scrivere</param>
        protected void WriteBlob(byte[] buffer)
        {
            // Lunghezza dei dati da scrivere
            int nbytes = buffer.Length;

            // Se supera la lunghezza massima lancio un'eccezione
            if (nbytes > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("buffer");

            // Scrivo il numero di byte
            WriteShort((ushort)nbytes);
            // Scrivo la stringa
            dataStream.Write(buffer, 0, nbytes);
        }

        /// <summary>
        /// Legge una lista di stringhe dal messaggio
        /// </summary>
        /// <returns>La lista di stringhe lette</returns>
        protected IList<string> ReadStringList()
        {
            List<String> l = new List<string>();

            // Leggo il numero di elementi
            uint n = ReadInteger();

            // Leggo le stringhe della lista
            for (int i = 0; i < n; i++)
                l.Add(ReadString());

            return l;
        }

        /// <summary>
        /// Scrive nel messaggio una lista di stringe
        /// </summary>
        /// <param name="list">Lista di stringhe da scrivere</param>
        protected void WriteStringList(IList<string> list)
        {
            // Scrivo il numero di elementi
            uint n = (uint)list.Count;
            WriteInteger(n);
            
            // Scrivo le stringhe
            foreach (string currentString in list)
                WriteString(currentString);
        }
        
        #endregion

        private void WriteHeader()
        {
            // SFSP
            dataStream.Write(new byte[] { 0x53, 0x46, 0x53, 0x50 }, 0, 4);

            // Versione
            dataStream.WriteByte(MY_PROTOCOL_VERSION_MAJOR);
            dataStream.WriteByte(MY_PROTOCOL_VERSION_MINOR);

            // Tipo di messaggio
            WriteShort((ushort)_MessageType);

            // Lascio lo spazio per inserire in seguito la dimensione del messaggio
            dataStream.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
        }

        /// <summary>
        /// Converte il messaggio in un vettore di byte pronto per essere memorizzato o inviato in rete
        /// </summary>
        /// <returns>Vettore di byte rappresentante il messaggio</returns>
        public byte[] GetBytes()
        {
            // Inizializzo un memory stream per scrivere il messaggio
            MemoryStream ms = new MemoryStream(12); // 12 byte per l'header
            dataStream = ms;

            // Scrivo l'header
            WriteHeader();

            // Scrivo i dati (questo metodo è abstract, chiamato sulle sottoclassi)
            WriteData();

            // Scrivo la lunghezza del messaggio nell'header
            ms.Seek(8, SeekOrigin.Begin);
            WriteInteger((uint)ms.Length - 12);

            // Restituisco l'array di byte rappresentante il messaggio
            byte[] buffer = ms.ToArray();
            ms.Dispose();
            ms = null;
            return buffer;
        }

        /// <summary>
        /// Scrive su uno stream il contenuto del messaggio
        /// </summary>
        /// <param name="stream">Stream su cui scrivere il messaggio</param>
        public void Write(Stream stream)
        {
            byte[] buf = GetBytes();
            stream.Write(buf, 0, buf.Length);
        }

        /// <summary>
        /// Utilizzata dalle classi derivate per scrivere il contenuto del messaggio
        /// </summary>
        protected abstract void WriteData();

        /// <summary>
        /// Utilizzata dalle classi derivate per leggere il contenuto del messaggio
        /// </summary>
        protected abstract void ReadData();

        /// <summary>
        /// Major number della versione del protocollo SFSP
        /// </summary>
        public int ProtocolVersionMajor
        {
            get
            {
                return _protocolVersionMajor;
            }
        }

        /// <summary>
        /// Minor number della versione del protocollo SFSP
        /// </summary>
        public int ProtocolVersionMinor
        {
            get
            {
                return _protocolVersionMinor;
            }
        }

        /// <summary>
        /// Versione del protocollo SFSP
        /// </summary>
        public string ProtocolVersion
        {
            get
            {
                return _protocolVersionMajor.ToString() + "." + _protocolVersionMinor.ToString();
            }
        }


        /// <summary>
        /// Tipo di messaggio
        /// </summary>
        public SfspMessageTypes MessageType
        {
            get
            {
                return _MessageType;
            }
        }
    }
}
