using Soso.Net.Models;

namespace Soso.Net
{
	public interface IUserConnection
	{
		public ulong Id { get; }
		public void Send<T>(T data, int channel, SOSO_SEND_TYPE sendType);
	}
}
