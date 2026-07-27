namespace Soso.Net.Components.NetworkedBodies.Snapshots
{
    public interface ISnapshot
    {
        public double LocalTime { get; set; }
        public double RemoteTime { get; set; }
    }
}