using Soso.Net.Stream;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Soso.Net.Transports.UDP
{
	public class SosoUdpServer : ISocketListener
	{
		private Action<ISocketConnection> _onConnection;
		public readonly Socket _socket;
		private readonly ByteBuffer _byteProcessor;
		private readonly Dictionary<EndPoint, SosoUdpConnection> _connections = new Dictionary<EndPoint, SosoUdpConnection>();
		
		public SosoUdpServer(EndPoint ep)
		{
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_byteProcessor = new ByteBuffer(1 * 1024 * 1024);
			_socket.Bind(ep);
		}

		public void Process()
		{
			while (_socket.Available > 0)
			{
				EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
				int received;

				try
				{
					received = _socket.ReceiveFrom(_byteProcessor.Buffer, ref remoteEp);
				}
				catch (SocketException)
				{
					continue;
				}

				if (_connections.TryGetValue(remoteEp, out var connection) == false)
				{
					// New connection
					connection = new SosoUdpConnection(remoteEp, false);
					connection.SetHandler(_byteProcessor);
					_connections[remoteEp] = connection;
					_onConnection(connection);
				}
				connection.OnReceived(new Span<byte>(_byteProcessor.Buffer, _byteProcessor.Position, received));
				_byteProcessor.Flush();
			}
		}

		public void Connected(Action<ISocketConnection> connection)
		{
			_onConnection = connection;
		}
		public void Shutdown()
		{
			_connections.Clear();
			_socket.Close();
		}
	}
}
