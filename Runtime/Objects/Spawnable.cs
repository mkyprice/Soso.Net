using System;
using Soso.Net.Behaviors;

namespace Soso.Net.Objects
{
    [Serializable]
    public struct Spawnable<TEnum>
        where TEnum : unmanaged, Enum
    {
        public NetworkIdentity Prefab;
        public TEnum TypeId;
    }
}