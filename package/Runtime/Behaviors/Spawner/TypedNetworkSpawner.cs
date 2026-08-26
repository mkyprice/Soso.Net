using System;
using Soso.Net.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Behaviors
{
	[Serializable]
	public abstract class TypedNetworkSpawner<TEnum> : INetworkSpawner
		where TEnum : unmanaged, Enum
	{
		#region Public Methods

		protected override string ToString(int? spawnType)
		{
			var enumType = ((ulong?)spawnType)?.ToEnum<TEnum>();
			return enumType != null ? enumType.ToString() : "?";
		}

		public NetworkIdentity Spawn(Scene scene, TEnum spawnType, Vector3 position, Quaternion rotation)
		{
			var typeValue = (int)spawnType.ToValue();
			return base.Spawn(scene, typeValue, position, rotation);
		}

		public NetworkIdentity LoadSpawn(Scene scene, TEnum spawnType, Vector3 position, Quaternion rotation)
		{
			var typeValue = (int)spawnType.ToValue();
			return LoadSpawn(scene, typeValue, position, rotation);
		}

		#endregion

		protected override void Spawn(int? spawnType, NetworkIdentity identity)
		{
			var enumType = ((ulong?)spawnType)?.ToEnum<TEnum>();
			Spawn(enumType, identity);
		}

		protected virtual void Spawn(TEnum? spawnType, NetworkIdentity identity)
		{
		}
	}
}
