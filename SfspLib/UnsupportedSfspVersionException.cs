using System;

namespace Sfsp
{
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
