using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Utils
{
    public class MessageSerializer
    {
        public byte[] SendBuffer;

        public MessageSerializer(int bufferSize = 1 * 1024 * 1024)
        {
            SendBuffer = new byte[bufferSize];
        }
        
        public int Write<T>(T message)
        {
            ByteWriter writer = new ByteWriter(SendBuffer);
            
            SosoSerializer.Serialize(ref writer, message, SerializationFlags.EmbedType);

            int length = writer.Position;

            return length;
        }
    }
}