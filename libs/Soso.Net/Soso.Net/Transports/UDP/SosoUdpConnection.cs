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
		private readonly bool _ownsSocket;

		public SosoUdpConnection(EndPoint ep, bool ownsSocket) : this(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp), ep, ownsSocket)
		{
		}
		
		public SosoUdpConnection(Socket socket, EndPoint ep, bool ownsSocket)
		{
			_ownsSocket = ownsSocket;
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
			if (_ownsSocket == false)
			{
				// We are server side and this is not how we process
				return;
			}
			while (Socket.Available > 0)
			{
				EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
				int received = Socket.ReceiveFrom(_byteProcessor.Buffer, ref ep);
				_byteProcessor.Receive(received);
			}
		}

		// Server call for receiving
		internal void OnReceived(Span<byte> received)
		{
			received.CopyTo(_byteProcessor.Buffer);
			_byteProcessor.Receive(received.Length);
		}
		
		public bool Poll()
		{
			return true;
		}

		public void Shutdown()
		{
			if (_ownsSocket)
			{
				Socket.Close();
			}
		}
	}
}
