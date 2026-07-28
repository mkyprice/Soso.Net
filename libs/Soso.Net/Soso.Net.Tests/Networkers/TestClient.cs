using Soso.Net.Transports;
using System.Text;

namespace Soso.Net.Tests.Networkers
{
	public class TestClient : SosoConnectionManager
	{
		public ulong ReceiveCount = 0;
		public bool LogMessage = false;

		public TestClient(ulong id, ISocketConnection socketConnection) : base(id, socketConnection)
		{
		}
		
		public void Send(string message, int channel)
		{
			var bytes = Encoding.ASCII.GetBytes(message);
			Send(bytes, channel);
		}

		public override void OnMessage(ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
		{
			string message =  Encoding.ASCII.GetString(data);
			if (LogMessage) Console.WriteLine($"Conn:{Connection.Id}Channel:{channel}Time:{DateTime.FromBinary(timestamp)}Num:{messageNumber} - {message}");
			ReceiveCount++;
		}
		public override void OnStateChanged(CONNECTION_STATUS status)
		{
			
		}
	}
}
