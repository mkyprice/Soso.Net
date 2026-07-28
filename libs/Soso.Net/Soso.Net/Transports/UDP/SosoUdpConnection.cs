using Soso.Net.Stream;
using System;
using System.Net;
using System.Net.Sockets;
using Soso.Net.Logging;

namespace Soso.Net.Transports.UDP
{
	public class SosoUdpConnection : ISocketConnection
	{
		public readonly Socket Socket;
		public readonly EndPoint EndPoint;
		private ByteBuffer _byteProcessor;

		public SosoUdpConnection(EndPoint ep) : this(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp), ep)
		{
		}
		
		public SosoUdpConnection(Socket socket, EndPoint ep)
		{
			Socket = socket;
			EndPoint = ep;
		}
		
		public void SetHandler(ByteBuffer processor)
		{
			_byteProcessor = processor;
		}
		
		public int Send(byte[] bytes, int offset, int count)
		{
			return Socket.SendTo(bytes, offset, count, SocketFlags.None, EndPoint);
		}
		public void Process()
		{
			if (Socket.Available > 0)
			{
				EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
				int received = Socket.ReceiveFrom(_byteProcessor.Buffer, ref ep);
				if (received == 0)
				{
					// Shutdown
				}
				_byteProcessor.Receive(received);
			}
		}

		public bool Poll()
		{
			NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "There's no point in polling for UDP connection... Stop it...");
			return true;
		}

		public void Shutdown()
		{
			Socket.Shutdown(SocketShutdown.Both);
			Socket.Close();
		}
	}
}
