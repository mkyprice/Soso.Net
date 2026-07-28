using Soso.Net.Transports;
using System.Text;

namespace Soso.Net.Tests.Networkers
{
	public class TestListener : SosoSocketManager
	{
		public ulong ReceiveCount = 0;
		private bool LogMessage = false;

		public TestListener(ISocketListener listener) : base(listener)
		{
		}
		
		public void Broadcast(string message, int channel)
		{
			var bytes = Encoding.ASCII.GetBytes(message);
			Broadcast(bytes, channel);
		}

		public SosoSocket GetConnection(ulong id)
		{
			foreach (var socket in Listener.Sockets)
			{
				if (socket.State.Id == id)
				{
					return socket;
				}
			}
			return null;
		}

		public override bool AcceptConnection(SosoSocket connection)
		{
			return true;
		}

		public override void ConnectionChanged(SosoSocket connection, CONNECTION_STATUS status)
		{
		}

		public override void OnMessage(SosoSocket connection, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
		{
			string message =  Encoding.ASCII.GetString(data);
			if (LogMessage) Console.WriteLine($"Conn:{connection.GetConnectionId()}Channel:{channel}Time:{timestamp}Num:{messageNumber} - {message}");
			ReceiveCount++;
		}
	}
}
