using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Soso.Net.Logging;
using Soso.Net.Transports.Extensions;

namespace Soso.Net.Transports.TCP
{
    public class SosoTcpListener : ISocketListener
    {
        private Action<ISocketConnection> _onConnection;
		
        public readonly Socket Listener;
        private readonly List<SosoTcpConnection> _connections = new List<SosoTcpConnection>();

        public SosoTcpListener()
        {
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.LingerState = new LingerOption(false, 0);
            Listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public void StartListener(EndPoint ep, int backlog)
        {
            Listener.Bind(ep);
            Listener.Listen(backlog);
            BeginAccept();
        }

        public void Connected(Action<ISocketConnection> connection)
        {
            _onConnection = connection;
        }
        
        public void Shutdown()
        {
            try
            {
                // Listener.Shutdown(SocketShutdown.Both);
                // Listener.Disconnect(false);
                Listener.Close();
            }
            catch (Exception e)
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Shutdown listener failed with error: {message}", e.Message);
            }
        }

        private void BeginAccept()
        {
            Listener.BeginAccept(OnAccept, Listener);
        }
		
        private void OnAccept(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket connectionSock = listener.EndAccept(ar);
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Accepted new connection");
            SosoTcpConnection connection = new SosoTcpConnection(connectionSock);
            _connections.Add(connection);
            _onConnection(connection);
            BeginAccept();
        }
    }
}