using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Models.Packets
{
	public struct NetworkMessage : INetworkMessage
	{
		public bool SyncTime { get; set; }
		public DESTINATION Destination { get; set; }
		public double Time { get; set; }
		public NetworkInstanceId SourceInstance { get; set; }
		public SOSO_SEND_TYPE SendType;
		public ushort Channel;
		public object Data;

		public override string ToString()
		{
			return $"{nameof(NetworkMessage)}: Src: {SourceInstance} Chl: {Channel} Data: {Data}";
		}

		public class Serializer : ISerializer<NetworkMessage>
		{
			public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
			{
				Serialize(ref writer, (NetworkMessage)value, config);
			}

			public NetworkMessage Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				NetworkMessage value = new NetworkMessage();
				value.SyncTime = reader.ReadBool();
				value.Destination = (DESTINATION)reader.ReadByte();
				value.SendType = (SOSO_SEND_TYPE)reader.ReadByte();
				value.Time = reader.ReadDouble();
				value.SourceInstance = (NetworkInstanceId)reader.ReadULong();
				reader.Read(out value.Channel);
				value.Data = SosoSerializer.Deserialize(ref reader, config);
				return value;
			}

			public void Serialize(ref ByteWriter writer, NetworkMessage value, SerializationConfig config)
			{
				writer.Write(value.SyncTime);
				writer.Write((byte)value.Destination);
				writer.Write((byte)value.SendType);
				writer.Write(value.Time);
				writer.Write(value.SourceInstance);
				writer.Write(value.Channel);
				SosoSerializer.Serialize(ref writer, value.Data, config, SerializationFlags.EmbedType);
			}

			object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				return Deserialize(ref reader, config);
			}
		}
	}
}
