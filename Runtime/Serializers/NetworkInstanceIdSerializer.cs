using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Serializers
{
	public struct NetworkInstanceIdSerializer : ISerializer<NetworkInstanceId>
	{
		public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
		{
			Serialize(ref writer, (NetworkInstanceId)value, config);
		}
		public NetworkInstanceId Deserialize(ref ByteReader reader, SerializationConfig config)
		{
			return (NetworkInstanceId)reader.ReadULong();
		}
		public void Serialize(ref ByteWriter writer, NetworkInstanceId value, SerializationConfig config)
		{
			writer.WriteBlittable(value);
		}
		object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
		{
			return Deserialize(ref reader, config);
		}
	}
}
