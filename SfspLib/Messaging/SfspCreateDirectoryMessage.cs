using System;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Rappresenta un messaggio di tipo CreateDirectory, attraverso il quale si chiede al ricevente
    /// di creare una cartella
    /// </summary>
    internal class SfspCreateDirectoryMessage : SfspMessage
    {
        private string _RelativePath;

        public SfspCreateDirectoryMessage(string relativePath) : base(SfspMessageTypes.CreateDirectory)
        {
            _RelativePath = relativePath;
        }

        protected override void ReadData()
        {
            _RelativePath = ReadString();
        }

        protected override void WriteData()
        {
            WriteString(_RelativePath);
        }

        /// <summary>
        /// Percorso relativo (alla radice del trasferimento) della cartella da creare
        /// </summary>
        public string RelativePath
        {
            get
            {
                return _RelativePath;
            }
            set
            {
                _RelativePath = value;
            }
        }
    }
}
