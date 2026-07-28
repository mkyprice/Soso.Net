using Soso.Net.Transports;
using System;

namespace Soso.Net
{
	public abstract class SosoSocketManager : ISocketManager
	{
		protected readonly SosoListener Listener;

		protected SosoSocketManager(ISocketListener listener)
		{
			Listener = new SosoListener(listener, this);
		}

		public void Broadcast(Span<byte> data, int channel)
		{
			foreach (SosoSocket socket in Listener.Sockets)
			{
				socket.Send(data, channel, 0);
			}
		}

		public void Shutdown()
		{
			Listener.Shutdown();
		}

		public abstract bool AcceptConnection(SosoSocket connection);

		public abstract void ConnectionChanged(SosoSocket connection, CONNECTION_STATUS status);

		public abstract void OnMessage(SosoSocket connection, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber);
	}
}
