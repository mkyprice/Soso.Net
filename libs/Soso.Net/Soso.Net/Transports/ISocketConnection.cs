using Soso.Net.Stream;
using System;

namespace Soso.Net.Transports
{
	public interface ISocketConnection
	{
		public void SetHandler(ByteBuffer processor);
		public int Send(byte[] bytes, int offset, int count);
		public void Shutdown();
		public void Process();
		public bool Poll();
	}
}
