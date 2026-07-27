using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;
using UnityEngine;

namespace Soso.Net.Serializers
{
	public struct Vector2Serializer : ISerializer<Vector2>
	{
		public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
		{
			Serialize(ref writer, (Vector2)value, config);
		}
		public Vector2 Deserialize(ref ByteReader reader, SerializationConfig config)
		{
			return reader.ReadBlittable<Vector2>();
		}
		public void Serialize(ref ByteWriter writer, Vector2 value, SerializationConfig config)
		{
			writer.WriteBlittable(value);
		}
		object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
		{
			return Deserialize(ref reader, config);
		}
	}
}
