using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Models.Packets
{
    public struct SyncCall : INetworkMessage
    {
        public DESTINATION Destination { get; set; }
        public NetworkInstanceId SourceInstance { get; set; }
        public double Time { get; set; }
        public bool SyncTime { get; set; }
        
        // Sync
        public ushort TargetId;
        public int SyncId;
        public object Arg;

        public class Serializer : SosoSerializer<SyncCall>
        {
            public override SyncCall Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                SyncCall call = new SyncCall();
                // Network Message
                call.Destination = (DESTINATION)reader.ReadByte();
                call.SourceInstance = (NetworkInstanceId)reader.ReadULong();
                call.Time = reader.ReadDouble();
                call.SyncTime = reader.ReadBool();
                
                // Sync
                reader.Read(out call.TargetId);
                reader.Read(out call.SyncId);
                var arg = SosoSerializer.Deserialize(ref reader, config);
                call.Arg = arg;
                return call;
            }

            public override void Serialize(ref ByteWriter writer, SyncCall value, SerializationConfig config)
            {
                // Network Message
                writer.Write((byte)value.Destination);
                writer.Write(value.SourceInstance);
                writer.Write(value.Time);
                writer.Write(value.SyncTime);
                
                // Sync
                writer.Write(value.TargetId);
                writer.Write(value.SyncId);
                SosoSerializer.Serialize(ref writer, value.Arg, config, SerializationFlags.EmbedType);
            }
        }
    }
}