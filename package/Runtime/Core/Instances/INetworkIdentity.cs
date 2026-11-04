using Soso.Net.Models.Packets;
using Soso.Net.Objects;

namespace Soso.Net.Core.Instances
{
    public interface INetworkIdentity
    {
        bool IsInitialized { get; }
        string GetName();
        void Initialize(NetworkInstance instance);
        void HandleMessage(INetworkMessage message);
    }
}