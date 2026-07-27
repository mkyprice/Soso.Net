using System;
using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Utils;

namespace Soso.Net.Models.Packets
{
    [Serializable]
    public struct RpcCall : INetworkMessage
    {
        public DESTINATION Destination { get; set; }
        public NetworkInstanceId SourceInstance { get; set; }
        public double Time { get; set; }
        public bool SyncTime { get; set; }
        
        public ushort TargetId;
        public ushort SourceId;
        public int Method;
        public object[] Args;

        public override string ToString()
        {
            return $"RpcCall({SourceId}:{Method}) {(Args == null ? "null" : string.Join(", ", Args.Select(a => a?.GetType().Name)))}";
        }

        public class Serializer : SosoSerializer<RpcCall>
        {
            public override RpcCall Deserialize(ref ByteReader reader, SerializationConfig config)
            {
                RpcCall call = new RpcCall();
                // Network Message
                call.Destination = (DESTINATION)reader.ReadByte();
                call.SourceInstance = (NetworkInstanceId)reader.ReadULong();
                call.Time = reader.ReadDouble();
                call.SyncTime = reader.ReadBool();
                
                // Rpc
                reader.Read(out call.TargetId);
                reader.Read(out call.SourceId);
                reader.Read(out call.Method);
                int argCount = reader.ReadByte();
                call.Args = new object[argCount];
                for (int i = 0; i < call.Args.Length; i++)
                {
                    var arg = SosoSerializer.Deserialize(ref reader, config);
                    call.Args[i] = arg;
                }
                return call;
            }

            public override void Serialize(ref ByteWriter writer, RpcCall value, SerializationConfig config)
            {
                // Network Message
                writer.Write((byte)value.Destination);
                writer.Write(value.SourceInstance);
                writer.Write(value.Time);
                writer.Write(value.SyncTime);
                
                // Rpc
                writer.Write(value.TargetId);
                writer.Write(value.SourceId);
                writer.Write(value.Method);
                writer.Write((byte)(value.Args?.Length ?? 0));
                for (int i = 0; i < value.Args?.Length; i++)
                {
                    SosoSerializer.Serialize(ref writer, value.Args[i], config, SerializationFlags.EmbedType);
                }
            }
        }
    }
}