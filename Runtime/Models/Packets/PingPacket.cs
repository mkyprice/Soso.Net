using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Models.Packets
{
    public struct PingPacket
    {
        public ushort SourceId;
        public double SendTime;
        public long RealtimeSendTicks;
        
        public class Serializer : ISerializer<PingPacket>
        {
            public PingPacket Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                var value = reader.ReadBlittable<PingPacket>();
                return value;
            }

            public void Serialize(ref ByteWriter writer, PingPacket value, SerializationConfig config)
            {
                writer.WriteBlittable(value);
            }
            
            public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
            {
                Serialize(ref writer, (PingPacket)value, config);
            }

            object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                return Deserialize(ref reader, config);
            }
        }
    }
}