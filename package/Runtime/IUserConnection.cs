using Soso.Net.Models;

namespace Soso.Net
{
	public interface IUserConnection
	{
		public ulong Id { get; }
		public void Send(byte[] data, int offset, int length, SOSO_SEND_TYPE sendType, int lane);
	}
}
