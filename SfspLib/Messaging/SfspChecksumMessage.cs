using System;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Questa classe è utilizzata per rappresentare un messaggio Sfsp contenente un checksum (più precisamente un hash SHA256)
    /// Fornisce un metodo Check() per confrontare il checksum contenuto con un altro
    /// </summary>
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
                // L'hash SHA256 è lungo 32 byte, non accettiamo niente che non abbia quella lunghezza
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
            // Se le dimensioni sono diverse qualcosa non quadra, comunque sicuramente gli hash sono diversi
            if (secondHash.Length != 32)
                return false;

            // Scorro tutti i byte dei due hash, se trovo una differenza restituisco false
            for (int i = 0; i < 32; i++)
                if (_Sha256[i] != secondHash[i])
                    return false;

            // Tutto a posto
            return true;
        }
    }
}
