using Soso.Net.Models.Packets;
using Soso.Net.Objects;

namespace Soso.Net.Core.Instances
{
    public interface INetworkIdentity
    {
        string GetName();
        void Initialize(NetworkInstance instance);
        bool HandleMessage(INetworkMessage message);
    }
}