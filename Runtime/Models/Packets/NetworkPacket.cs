using System;
using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Models.Packets
{
	[Serializable]
	public struct NetworkPacket
	{
		public ushort SourceId;
		public double SendTime;
		public INetworkMessage[] Messages;
		
		
		public class Serializer : SosoSerializer<NetworkPacket>
		{
			public override void Serialize(ref ByteWriter writer, NetworkPacket value, SerializationConfig config)
			{
				int count = value.Messages.Length;
				writer.Write(value.SourceId);
				writer.Write(value.SendTime);
				writer.Write((ushort)count);
				foreach (var message in value.Messages)
				{
					SosoSerializer.Serialize(ref writer, message, config, SerializationFlags.EmbedType);
				}
			}
			public override NetworkPacket Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				NetworkPacket packet = new NetworkPacket();
				reader.Read(out packet.SourceId);
				reader.Read(out packet.SendTime);
				int count = reader.ReadUShort();
				packet.Messages = new INetworkMessage[count];
				for (int i = 0; i < count; i++)
				{
					var message = SosoSerializer.Deserialize(ref reader, config);
					packet.Messages[i] = message as INetworkMessage;
				}
				return packet;
			}
		}
	}
}
