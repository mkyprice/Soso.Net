using Soso.Net.Logging;
using Soso.Net.Packets;
using Soso.Net.Transports;
using System;

namespace Soso.Net
{
	public interface IConnectionManager
	{
		void OnStateChanged(CONNECTION_STATUS status);
		void OnMessage(ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber);
	}
	
	public class SosoConnection : ISocketProcessor
	{
		public ulong Id => Connection.State.Id;
		public readonly SosoSocket Connection;
		public CONNECTION_STATUS Status
		{
			get => Connection.State.Status;
			set
			{
				Connection.SetState(value);
				_connectionManager.OnStateChanged(Connection.State.Status);
			}
		}
		private readonly IConnectionManager _connectionManager;
		
		internal SosoConnection(ulong id, ISocketConnection connection, IConnectionManager manager)
		{
			_connectionManager = manager;
			Connection = new SosoSocket(connection, this);
			Connection.State.SetId(id);
			Connect();
		}

		private void Connect()
		{
			Status = CONNECTION_STATUS.Connecting;
			var negotiation = new Negotiation();
			negotiation.Id = Id;
			Connection.Send(negotiation.ToBytes(), 0, 1);
		}

		public void Shutdown()
		{
			Connection.Shutdown();
		}

		public void Send(Span<byte> data, int channel)
		{
			if (Status != CONNECTION_STATUS.Connected)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Send failed - you are not connected. Status is {Status}", Status);
				return;
			}
			Connection.Send(data, channel, 0);
		}

		public void OnStateChanged(SosoSocket socket, CONNECTION_STATUS status)
		{
			_connectionManager.OnStateChanged(status);
		}

		/// <summary>
		/// When a message is received
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="packetType"></param>
		/// <param name="data"></param>
		/// <param name="channel"></param>
		/// <param name="timestamp">UTC ticks</param>
		/// <param name="messageNumber"></param>
		public void OnMessage(SosoSocket socket, int packetType, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
		{
			if (Status == CONNECTION_STATUS.Connecting)
			{
				if (packetType == 1)
				{
					Negotiation negotiation = Negotiation.FromBytes(data);
					if (negotiation.Accepted)
					{
						Status = CONNECTION_STATUS.Connected;
					}
				}
				else
				{
					NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Received packet during connecting phase");
				}
			}
			else
			{
				_connectionManager.OnMessage(data, channel, timestamp, messageNumber);
			}
		}
	}
}
