using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Models.Packets
{
	public struct UnreliablePacket
	{
		public INetworkMessage Message;
		
		public class Serializer : ISerializer<UnreliablePacket>
		{
			public void Serialize(ref ByteWriter writer, UnreliablePacket value, SerializationConfig config)
			{
				SosoSerializer.Serialize(ref writer, value.Message, SerializationFlags.EmbedType, config);
			}
			public UnreliablePacket Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				var value = new UnreliablePacket();
				value.Message = SosoSerializer.Deserialize(ref reader, config) as INetworkMessage;
				return value;
			}
		}
	}
}
