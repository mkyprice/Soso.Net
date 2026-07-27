using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Models.Packets
{
    public struct RequestPacket
    {
        public int RequestId;
        public string Path;
        public object[] Args;
        
        public class Serializer : SosoSerializer<RequestPacket>
        {
            public override RequestPacket Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                var value = new RequestPacket();
                reader.Read(out value.RequestId);
                reader.Read(out value.Path);
                int argCount = reader.ReadByte();
                value.Args = new object[argCount];
                for (int i = 0; i < value.Args.Length; i++)
                {
                    var arg = SosoSerializer.Deserialize(ref reader, config);
                    value.Args[i] = arg;
                }
                return value;
            }

            public override void Serialize(ref ByteWriter writer, RequestPacket value, SerializationConfig config)
            {
                writer.Write(value.RequestId);
                writer.Write(value.Path);
                writer.Write((byte)(value.Args?.Length ?? 0));
                for (int i = 0; i < value.Args?.Length; i++)
                {
                    SosoSerializer.Serialize(ref writer, value.Args[i], config, SerializationFlags.EmbedType);
                }
            }
        }
    }
}