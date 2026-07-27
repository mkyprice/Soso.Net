namespace Soso.Net.Models.Packets
{
	public interface INetworkMessage
	{
		public DESTINATION Destination { get; }
		public NetworkInstanceId SourceInstance { get; set; }
		public double Time { get; set; }
		public bool SyncTime { get; }
	}
}
