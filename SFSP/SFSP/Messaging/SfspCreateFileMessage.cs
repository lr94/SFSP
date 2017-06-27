using System;

namespace SFSP.Messaging
{
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
        /// Percorso del file, relativo alla directory radice trasferita
        /// </summary>
        public string FileRelativePath
        {
            get;
            set;
        }
    }
}
