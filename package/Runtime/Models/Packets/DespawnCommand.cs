using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;

namespace Soso.Net.Models.Packets
{
	public struct DespawnCommand : INetworkMessage
	{
		public DESTINATION Destination => DESTINATION.Client;
		public NetworkInstanceId SourceInstance
		{
			get => NetworkInstanceId.Spawner;
			set
			{
			}
		}
		public double Time { get; set; }
		public bool SyncTime => true;
		public ushort SourceId;
		public NetworkInstanceId Id;

		public class Serializer : ISerializer<DespawnCommand>
		{
			public DespawnCommand Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				DespawnCommand value = reader.ReadBlittable<DespawnCommand>();
				return value;
			}

			public void Serialize(ref ByteWriter writer, DespawnCommand value, SerializationConfig config)
			{
				writer.WriteBlittable(value);
			}
			
			public void Serialize(ref ByteWriter writer, object value, SerializationConfig config)
			{
				Serialize(ref writer, (DespawnCommand)value, config);
			}

			object ISerializer.Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				return Deserialize(ref reader, config);
			}
		}
	}
}
