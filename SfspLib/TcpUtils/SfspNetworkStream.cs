using System;
using System.Net.Sockets;

namespace Sfsp.TcpUtils
{
    internal class SfspNetworkStream : NetworkStream
    {
        public SfspNetworkStream(Socket socket) : base(socket)
        {
        }

        public SfspNetworkStream(Socket socket, bool ownSocket) : base(socket, ownSocket)
        {
        }

        public SfspNetworkStream(Socket socket, System.IO.FileAccess fileAccess) : base(socket, fileAccess)
        {
        }

        public SfspNetworkStream(Socket socket, System.IO.FileAccess fileAccess, bool ownSocket) : base(socket, fileAccess, ownSocket)
        {
        }

        /// <summary>
        /// Ottiene il socket sottostante
        /// </summary>
        /// <returns></returns>
        public Socket GetSocket()
        {
            return this.Socket;
        }
    }
}
