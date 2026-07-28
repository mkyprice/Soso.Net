using System;

namespace Soso.Net
{
    [Serializable]
    public struct NetworkInstanceId : IEquatable<NetworkInstanceId>
    {
        public ulong Id;
        
        public static readonly NetworkInstanceId Invalid = new NetworkInstanceId(0);
        public static readonly NetworkInstanceId Spawner = new NetworkInstanceId(0, ushort.MaxValue, 0);
        
        public ushort SessionId => (ushort)(Id >> 48);
        public ushort SceneId => (ushort)(Id >> 32);
        public ulong SequenceNumber => Id & SEQUENCE_MASK;
        
        private const ulong SEQUENCE_MASK = 0xFFFFFFFF;

        public NetworkInstanceId(ulong instanceId)
        {
            Id = instanceId;
        }
        
        public NetworkInstanceId(ushort sessionId,  ushort sceneId, ulong sequenceNumber)
        {
            Id = (((ulong)sessionId << 48) | ((ulong)sceneId << 32) | (sequenceNumber & SEQUENCE_MASK));
        }
        
        public bool Equals(NetworkInstanceId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"[Session: {SessionId} | SceneId: {SceneId} | Sequence: {SequenceNumber} | Id: {Id}]";
        }
        
        public static bool operator==(NetworkInstanceId a, NetworkInstanceId b) => a.Equals(b);
        public static bool operator !=(NetworkInstanceId a, NetworkInstanceId b) => !(a == b);

        public static implicit operator ulong(NetworkInstanceId netId) => netId.Id;
        public static explicit operator NetworkInstanceId(ulong instanceId) => new NetworkInstanceId(instanceId);
    }
}