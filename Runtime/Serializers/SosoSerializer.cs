using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Serializers
{
    public abstract class SosoSerializer<T> : ISerializer<T>
    {
        public abstract T Deserialize(ref ByteReader reader, SerializationConfig config);

        public abstract void Serialize(ref ByteWriter writer, T value, SerializationConfig config);
        
        public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
        {
            Serialize(ref writer, (T)value, config);
        }

        object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
        {
            return Deserialize(ref reader, config);
        }
    }
}