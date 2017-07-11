using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Sfsp.TcpUtils
{
    internal static class TcpUtilsExtension
    {

        /// <summary>
        /// Ottiene lo stato di una connessione TCP
        /// </summary>
        /// <param name="client">Oggetto TcpClient relativo alla connessione di cui si vuole conoscere lo stato</param>
        /// <returns>Lo stato della connessione</returns>
        public static TcpState GetState(this TcpClient client)
        {
            return client.Client.GetState();
        }

        /// <summary>
        /// Ottiene lo stato di una connessione TCP
        /// </summary>
        /// <param name="client">Oggetto Socket relativo alla connessione di cui si vuole conoscere lo stato</param>
        /// <returns>Lo stato della connessione</returns>
        public static TcpState GetState(this Socket client)
        {
            TcpConnectionInformation info = IPGlobalProperties.GetIPGlobalProperties()
                                                              .GetActiveTcpConnections()
                                                              .SingleOrDefault(t
                                                                 => t.LocalEndPoint.Equals(client.LocalEndPoint)
                                                                    &&
                                                                    t.RemoteEndPoint.Equals(client.RemoteEndPoint));

            if (info == null)
                return TcpState.Unknown;

            return info.State;
        }
    }
}
