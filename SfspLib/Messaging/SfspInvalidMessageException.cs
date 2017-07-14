using System;

namespace Sfsp.Messaging
{
    /// <summary>
    /// Eccezione lanciata quando viene ricevuto un messaggio non valido
    /// </summary>
    public class SfspInvalidMessageException : Exception
    {
        protected string _Message;

        public SfspInvalidMessageException() : base()
        {
            _Message = "Invalid SFSP message.";
        }

        public SfspInvalidMessageException(string message) : base(message)
        {
            _Message = message;
        }

        public SfspInvalidMessageException(string message, Exception innerException) : base(message, innerException)
        {
            _Message = message;
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
