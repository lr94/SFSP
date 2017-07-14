using System;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Rappresenta un messaggio CreateFile, inviato per dire al ricevente di creare un nuovo file e prepararsi a riceverlo.
    /// Specifica il nome (/percorso relativo) del file e la sua dimensione
    /// </summary>
    internal class SfspCreateFileMessage : SfspMessage
    {
        public SfspCreateFileMessage(ulong fileSize, string relativePath) : base(SfspMessageTypes.CreateFile)
        {
            FileSize = fileSize;
            FileRelativePath = relativePath;
        }

        protected override void ReadData()
        {
            FileSize = ReadLong();
            FileRelativePath = ReadString();
        }

        protected override void WriteData()
        {
            WriteLong(FileSize);
            WriteString(FileRelativePath);
        }

        /// <summary>
        /// Dimensione del file
        /// </summary>
        public ulong FileSize
        {
            get;
            set;
        }

        /// <summary>
        /// Percorso del file, relativo alla directory radice
        /// </summary>
        public string FileRelativePath
        {
            get;
            set;
        }
    }
}
