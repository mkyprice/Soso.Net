using Soso.Serialization;
using Soso.Serialization.Binary;
using UnityEngine;

namespace Soso.Net.Serializers
{
    public class QuaternionSerializer : SosoSerializer<Quaternion>
    {
        public override Quaternion Deserialize(ref ByteReader reader, SerializationConfig config)
        {
            Quaternion value = reader.ReadBlittable<Quaternion>();
            return value;
        }

        public override void Serialize(ref ByteWriter writer, Quaternion value, SerializationConfig config)
        {
            writer.WriteBlittable(value);
        }
    }
}