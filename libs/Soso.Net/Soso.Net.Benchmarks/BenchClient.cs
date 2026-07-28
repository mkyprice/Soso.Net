using Soso.Net.Transports;
using System.Text;
using Soso.Net.Logging;

namespace Soso.Net.Benchmarks
{
	public class BenchClient : SosoConnectionManager
	{
		public ulong ReceiveCount = 0;
		public bool LogMessage = true;

		public BenchClient(ulong id, ISocketConnection connection) : base(id, connection)
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
			if (LogMessage) NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Conn:{Connection.Id} Channel:{channel} Time:{timestamp} Num:{messageNumber} - {message}", Connection.Id, channel, timestamp, messageNumber, message);
			ReceiveCount++;
		}
		public override void OnStateChanged(CONNECTION_STATUS status)
		{
			
		}
	}
}
