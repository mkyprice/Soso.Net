using System;
using System.Collections.Generic;
using Soso.Net.Components.Spawning;
using Soso.Net.Extensions;
using Soso.Net.Models.Packets;
using Soso.Net.Objects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Behaviors
{
	[Serializable]
	public abstract class TypedNetworkSpawner<TEnum> : INetworkSpawner
		where TEnum : unmanaged, Enum
	{
		[SerializeField] public BaseSpawnList<TEnum> Prefabs;
		private Dictionary<Scene, GameObjectPool<TEnum>> _pools = new Dictionary<Scene, GameObjectPool<TEnum>>();

		private GameObjectPool<TEnum> GetPool(Scene scene)
		{
			if (_pools.TryGetValue(scene, out GameObjectPool<TEnum> pool) == false)
			{
				pool = new GameObjectPool<TEnum>(Prefabs);
				_pools[scene] = pool;
			}
			return pool;
		}
		
		#region Public Methods

		public override void Clear(Scene scene)
		{
			base.Clear(scene);

			if (_pools.TryGetValue(scene, out GameObjectPool<TEnum> pool))
			{
				pool.Cleanup();
				_pools.Remove(scene);
			}
		}

		protected override string ToString(ulong? spawnType)
		{
			var enumType = spawnType?.ToEnum<TEnum>();
			return enumType != null ? enumType.ToString() : "?";
		}

		public NetworkIdentity Spawn(Scene scene, TEnum spawnType, Vector3 position, Quaternion rotation)
		{
			var typeValue = spawnType.ToValue();
			return base.Spawn(scene, typeValue, position, rotation);
		}

		public NetworkIdentity LoadSpawn(Scene scene, TEnum spawnType, Vector3 position, Quaternion rotation)
		{
			var typeValue = spawnType.ToValue();
			return LoadSpawn(scene, typeValue, position, rotation);
		}

		#endregion

		protected override void DestroyIdentity(NetworkIdentity identity, NetworkInstanceData data)
		{
			var instanceType = data.GetType<TEnum>();
			var scene = identity.gameObject.scene;
			var pool = GetPool(scene);
			if (instanceType != null && pool.HasType(instanceType.Value))
			{
				pool.Return(identity, instanceType.Value);
			}
			else
			{
				identity.OnDespawn();
				DestroyImmediate(identity.gameObject);
			}

			OnDespawn(data.Id, identity);
		}

		protected override NetworkIdentity InstantiateIdentity(Scene scene, ulong spawnType, Vector3 position, Quaternion rotation)
		{
			var enumType = spawnType.ToEnum<TEnum>();
			var parent = SpawnerRegistry<TEnum>.GetParent(enumType);
			var pool = GetPool(scene);
			var inst = pool.Spawn(enumType, position, rotation, parent);
			return inst;
		}

		protected override bool Initialize(ulong? spawnType, NetworkIdentity identity, NetworkInstanceId instanceId)
		{
			if (base.Initialize(spawnType, identity, instanceId) == false)
			{
				return false;
			}
			
			var enumType = spawnType?.ToEnum<TEnum>();
			OnSpawn(enumType, identity);
			return true;
		}

		protected virtual void OnSpawn(TEnum? spawnType, NetworkIdentity identity)
		{
		}
		
		protected virtual void OnDespawn(NetworkInstanceId oldId, NetworkIdentity identity)
		{
		}
	}
}
