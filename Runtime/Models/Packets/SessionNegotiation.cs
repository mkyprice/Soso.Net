using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Models.Packets
{
    public struct SessionNegotiation
    {
        public SessionInfo Session;
        public SessionInfo[] ExistingUsers;
        
        public class Serializer : SosoSerializer<SessionNegotiation>
        {
            public override SessionNegotiation Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                var value = new SessionNegotiation();
                value.Session = reader.ReadBlittable<SessionInfo>();
                int count = reader.ReadByte();
                value.ExistingUsers = new SessionInfo[count];
                for (int i = 0; i < count; i++)
                {
                    value.ExistingUsers[i] = reader.ReadBlittable<SessionInfo>();
                }
                return value;
            }

            public override void Serialize(ref ByteWriter writer, SessionNegotiation value, SerializationConfig config)
            {
                writer.WriteBlittable(value.Session);
                writer.Write((byte)(value.ExistingUsers?.Length ?? 0));
                if (value.ExistingUsers != null)
                {
                    foreach (var session in value.ExistingUsers)
                    {
                        writer.WriteBlittable(session);
                    }
                }
            }
        }
    }
}