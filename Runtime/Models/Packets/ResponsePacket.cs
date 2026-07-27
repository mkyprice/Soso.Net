using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Models.Packets
{
    public struct ResponsePacket
    {
        public int RequestId;
        public string Request;
        public object Response;
        
        public class Serializer : SosoSerializer<ResponsePacket>
        {
            public override ResponsePacket Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                var value = new ResponsePacket();
                reader.Read(out value.RequestId);
                reader.Read(out value.Request);
                bool isResponseNull = reader.ReadBool();
                if (isResponseNull)
                {
                    value.Response = SosoSerializer.Deserialize(ref reader, config);
                }
                return value;
            }

            public override void Serialize(ref ByteWriter writer, ResponsePacket value, SerializationConfig config)
            {
                writer.Write(value.RequestId);
                writer.Write(value.Request);
                bool isResponseNull = value.Response != null;
                writer.Write(isResponseNull);
                if (isResponseNull)
                {
                    SosoSerializer.Serialize(ref writer, value.Response, config, SerializationFlags.EmbedType);
                }
            }
        }
    }
}