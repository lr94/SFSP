using System;

namespace Sfsp
{
    /// <summary>
    /// Eccezione sollevata quando l'host remoto utilizza una versione del protocollo Sfsp non supportata
    /// </summary>
    internal class UnsupportedSfspVersionException : Exception
    {
        protected string _Message;

        public UnsupportedSfspVersionException(int major, int minor) : base()
        {
            _Message = "SFSP " + major.ToString() + "." + minor.ToString() + " not supported";
        }

        public override string Message
        {
            get
            {
                return _Message;
            }
        }
    }
}
