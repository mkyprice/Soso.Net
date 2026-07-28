using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Soso.Net.Logging;
using Soso.Net.Stream;

namespace Soso.Net.Transports.TCP
{
	public class SosoTcpConnection : ISocketConnection
	{
		public readonly Socket Socket;
		private ByteBuffer _byteProcessor;

		public SosoTcpConnection()
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Socket.LingerState = new LingerOption(false, 0);
		}
		
		public SosoTcpConnection(Socket socket)
		{
			Socket = socket;
		}

		public void Connect(EndPoint ep)
		{
			Socket.Connect(ep);
		}

		public async Task ConnectAsync(EndPoint ep)
		{
			try
			{
				await Socket.ConnectAsync(ep);
			}
			catch (Exception e)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to connect with error: {message}", e.Message);
				throw;
			}
		}
		
		public void SetHandler(ByteBuffer processor)
		{
			_byteProcessor = processor;
		}
		
		public int Send(byte[] bytes, int offset, int count)
		{
			try
			{
				return Socket.Send(bytes, offset, count, SocketFlags.None);
			}
			catch (SocketException e)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Send failed with error code {errorCode}", e.ErrorCode);
				return 0;
			}
		}
		public void Shutdown()
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Shutting down socket...");
			Socket.Shutdown(SocketShutdown.Both);
			Socket.Disconnect(false);
			Socket.Close();
		}
		public void Process()
		{
			if (Socket.Available > 0)
			{
				int count = Socket.Receive(_byteProcessor.Buffer);
				_byteProcessor.Receive(count);
			}
		}

		public bool Poll()
		{
			try
			{
				bool isReadable = Socket.Poll(1, SelectMode.SelectRead);

				if (isReadable)
				{
					if (Socket.Available == 0)
					{
						return false;
					}
				}
			}
			catch (SocketException e)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default,"Poll failed with error code {errorCode}", e.ErrorCode);
				return false;
			}
			return true;
		}

		// private async Task ReceiveLoop()
		// {
		// 	while (_receiveCancellation.IsCancellationRequested == false)
		// 	{
		// 		int count = await _socket.ReceiveAsync(_buffer, SocketFlags.None, _receiveCancellation.Token);
		// 		
		// 		if (_receiveCancellation.IsCancellationRequested == false)
		// 		{
		// 			Span<byte> seg = new Span<byte>(_buffer, 0, count);
		// 			_byteProcessor.Append(seg);
		// 		}
		// 	}
		// 	Log.Info("Shut down receive loop");
		// }

		// private void BeginReceive()
		// {
		// 	_receiveCancellation = new CancellationTokenSource();
		// 	Task.Factory.StartNew(ReceiveLoop, TaskCreationOptions.LongRunning);
		// 	// _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceive, _socket);
		// }
		// private void OnReceive(IAsyncResult ar)
		// {
		// 	Socket socket = (Socket)ar.AsyncState;
		// 	int count = socket.EndReceive(ar);
		// 	var seg = new ReadOnlyMemory<byte>(_buffer, 0, count);
		// 	_byteProcessor(seg);
		// 	BeginReceive();
		// }
	}
}
