using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Models
{
    public struct SessionInfo
    {
        public ulong ConnectionId;
        public ushort SessionId;

        public override string ToString()
        {
            return $"Session({ConnectionId}:{SessionId})";
        }

        public override bool Equals(object obj)
        {
            return obj is SessionInfo session && session.ConnectionId == ConnectionId && session.SessionId == SessionId;
        }

        public override int GetHashCode()
        {
            return ConnectionId.GetHashCode();
        }

        public static bool operator ==(SessionInfo a, SessionInfo b)
        {
            return a.ConnectionId == b.ConnectionId && a.SessionId == b.SessionId;
        }

        public static bool operator !=(SessionInfo a, SessionInfo b)
        {
            return (a == b) == false;
        }

        public class Serializer : SosoSerializer<SessionInfo>
        {
            public override SessionInfo Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                return reader.ReadBlittable<SessionInfo>();
            }

            public override void Serialize(ref ByteWriter writer, SessionInfo value, SerializationConfig config)
            {
                writer.WriteBlittable(value);
            }
        }
    }
}