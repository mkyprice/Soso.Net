using Soso.Serialization;
using Soso.Serialization.Binary;
using System;

namespace Soso.Net.Utils
{
    public class MessageSerializer
    {
        public readonly struct Message
        {
            public readonly ushort Channel;
            public readonly object Data;
            public Message(object data, ushort channel)
            {
                Data = data;
                Channel = channel;
            }
        }
        
        public byte[] SendBuffer;

        public MessageSerializer(int bufferSize = 1 * 1024 * 1024)
        {
            SendBuffer = new byte[bufferSize];
        }
        
        public int Write<T>(T message, ushort channel)
        {
            ByteWriter writer = new ByteWriter(SendBuffer);

            writer.Write(channel);
            
            SosoSerializer.Serialize(ref writer, message, SerializationFlags.EmbedType);

            int length = writer.Position;

            return length;
        }

        public static Message Read(ReadOnlySpan<byte> bytes)
        {
            ByteReader reader = new ByteReader(bytes);

            ushort channel = reader.ReadUShort();
            object obj = SosoSerializer.Deserialize(ref reader);

            return new Message(obj, channel);
        }
    }
}