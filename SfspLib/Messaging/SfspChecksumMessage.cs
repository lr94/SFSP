using System;

namespace Sfsp.Messaging
{
    internal class SfspChecksumMessage : SfspMessage
    {
        byte[] _Sha256;

        public SfspChecksumMessage(byte[] sha256) : base(SfspMessageTypes.Checksum)
        {
            if (sha256.Length != 32)
                throw new ArgumentOutOfRangeException("sha256");

            _Sha256 = sha256;
        }

        protected override void ReadData()
        {
            _Sha256 = ReadBlob();
            if(_Sha256.Length != 32)
                throw new SfspInvalidMessageException("Invalid SHA256 length.");
            
        }

        protected override void WriteData()
        {
            WriteBlob(_Sha256);
        }

        /// <summary>
        /// Hash SHA256 dell'oggetto
        /// </summary>
        public byte[] Sha256
        {
            get
            {
                return _Sha256;
            }
            set
            {
                if (value.Length != 32)
                    throw new ArgumentException("Invalid SHA256 length.");

                _Sha256 = value;
            }
        }

        /// <summary>
        /// Confronta l'hash contenuto nel messaggio con un hash passato come argomento
        /// </summary>
        /// <param name="secondHash">Hash con cui effettuare il confronto</param>
        /// <returns></returns>
        public bool Check(byte[] secondHash)
        {
            if (secondHash.Length != 32)
                return false;

            for (int i = 0; i < 32; i++)
                if (_Sha256[i] != secondHash[i])
                    return false;

            return true;
        }
    }
}
