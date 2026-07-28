using System;

namespace Soso.Net.Transports
{
	public interface ISocketListener
	{
		public void Connected(Action<ISocketConnection> connection);
		public void Shutdown();
	}
}
