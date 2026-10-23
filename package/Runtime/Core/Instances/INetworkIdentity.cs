using Soso.Net.Models.Packets;

namespace Soso.Net.Objects
{
    public interface INetworkIdentity
    {
        string GetName();
        void Initialize(NetworkInstance instance);
        bool HandleMessage(INetworkMessage message);
    }
}