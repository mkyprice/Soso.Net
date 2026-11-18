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
        private volatile bool _isRunning = false;

        public SosoTcpListener()
        {
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.LingerState = new LingerOption(false, 0);
            Listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public void StartListener(EndPoint ep, int backlog)
        {
            _isRunning = true;
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
            _isRunning = false;
            try
            {
                // Listener.Shutdown(SocketShutdown.Both);
                // Listener.Disconnect(false);
                Listener.Close();
            }
            catch (Exception e)
            {
                NetworkLogger.Warn("Shutdown listener failed with error: {message}", e.Message);
            }
        }

        private void BeginAccept()
        {
            if (_isRunning == false)
            {
                return;
            }
            
            Listener.BeginAccept(OnAccept, Listener);
        }
		
        private void OnAccept(IAsyncResult ar)
        {
            if (_isRunning == false)
            {
                return;
            }
            Socket listener = (Socket)ar.AsyncState;
            Socket connectionSock;
            try
            {
                connectionSock = listener.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                // We called Close
                return;
            }
            catch (SocketException e)
            {
                if (_isRunning)
                {
                    NetworkLogger.Warn("EndAccept failed with socket error {code}: {message}", e.SocketErrorCode, e.Message);
                    BeginAccept();
                }
                return;
            }
            NetworkLogger.Info("Accepted new connection");
            SosoTcpConnection connection = new SosoTcpConnection(connectionSock);
            _connections.Add(connection);
            _onConnection(connection);
            BeginAccept();
        }
    }
}