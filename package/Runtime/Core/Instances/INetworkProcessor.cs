namespace Soso.Net.Core.Instances
{
    public interface INetworkProcessor
    {
        ushort GetLocalId();
        bool IsAuthority();
        ushort AuthorityId { get; }
    }
}