using Soso.Net.Transports;
using System.Net;
using System.Text;
using Soso.Net.Logging;

namespace Soso.Net.Benchmarks
{
	public class BenchListener : SosoSocketManager
	{
		public ulong ReceiveCount = 0;
		public int ConnectionCount = 0;
		public bool LogMessage = true;

		public BenchListener(ISocketListener listener) : base(listener)
		{
		}
		
		public void Broadcast(string message, int channel)
		{
			var bytes = Encoding.ASCII.GetBytes(message);
			Broadcast(bytes, channel);
		}

		public override bool AcceptConnection(SosoSocket connection)
		{
			return true;
		}

		public override void ConnectionChanged(SosoSocket connection, CONNECTION_STATUS status)
		{
			if (status == CONNECTION_STATUS.Connected)
			{
				ConnectionCount++;
			}
			else if (status == CONNECTION_STATUS.Disconnected)
			{
				ConnectionCount--;
			}
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Connection changed {connection.GetConnectionId()} - {status}", connection.GetConnectionId(), status);
		}

		public override void OnMessage(SosoSocket connection, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
		{
			string message =  Encoding.ASCII.GetString(data);
			if (LogMessage) NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Conn:{connection.GetConnectionId()} Channel:{channel} Time:{timestamp} Num:{messageNumber} - {message}", connection.GetConnectionId(), channel, timestamp, messageNumber, message);
			ReceiveCount++;
		}
	}
}
