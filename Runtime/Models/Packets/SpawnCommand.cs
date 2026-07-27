using System;
using Soso.Net.Serializers;
using Soso.Serialization;
using Soso.Serialization.Binary;
using Soso.Serialization.Serializers;
using UnityEngine;

namespace Soso.Net.Models.Packets
{
	[Serializable]
	public struct SpawnCommand : INetworkMessage
	{
		public DESTINATION Destination => DESTINATION.Client;
		public NetworkInstanceId SourceInstance
		{
			get => NetworkInstanceId.Spawner;
			set { }
		}
		public double Time { get; set; }
		public bool SyncTime => true;
		public ulong SpawnType;
		public ushort SceneId;
		public NetworkInstanceId Id;
		public Vector3 Position;
		public Quaternion Rotation;


		public class Serializer : SosoSerializer<SpawnCommand>
		{
			public override SpawnCommand Deserialize(ref ByteReader reader, SerializationConfig config)
			{
				SpawnCommand value = reader.ReadBlittable<SpawnCommand>();
				return value;
			}

			public override void Serialize(ref ByteWriter writer, SpawnCommand value, SerializationConfig config)
			{
				writer.WriteBlittable(value);
			}
		}
	}
}
