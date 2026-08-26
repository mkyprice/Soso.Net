using System;
using System.Collections.Generic;
using System.Linq;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.Spawning;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Models.Packets;
using Soso.Net.Objects;
using Soso.Net.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Behaviors
{
	[Serializable]
	// [RequireComponent(typeof(NetworkIdentity))]
	public class INetworkSpawner : BaseNetworkInstance
	{
		[SerializeField] public SpawnList Prefabs;
		
		public override bool IsOwner => true;
		protected ushort SessionId { get; private set; }
		public new NetworkController Network { get; private set; }
		
		private NetworkIdGenerator _idGenerator;
		private NetworkIdGenerator _serverObjectIdGenerator;
		
		private Dictionary<Scene, GameObjectPool> _pools = new Dictionary<Scene, GameObjectPool>();

		public override void Despawn()
		{
			NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{name} was called on the spawner", nameof(Despawn));
		}
		public override void DespawnLocal()
		{
			NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{name} was called on the spawner", nameof(DespawnLocal));
		}

		protected override void Start()
		{
			base.Start();
			
			if (TryGetComponent(out INetworkManager _) == false)
			{
				DestroyImmediate(gameObject);
				return;
			}
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		protected virtual void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			
			Shutdown();
		}
		
		public void Reset()
		{
			DeleteData(NetworkInstanceId.Spawner, out _);
			ResetInstance();
		}

		public void InitializeSpawner()
		{
			if (IsInitialized)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Spawner is already initialized");
				return;
			}

			InstanceId = NetworkInstanceId.Spawner;
			Network = INetworkManager.GetInstance().Network;
			SessionId = INetworkManager.GetInstance().Session.Session?.SessionId ?? 0;
			
			_idGenerator = new NetworkIdGenerator(SessionId);
			_serverObjectIdGenerator = new NetworkIdGenerator(0);
			
			if (CreateInstanceData(null, this, NetworkInstanceId.Spawner) == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not register {name}", name);
			}
		}
		

		public virtual void Shutdown()
		{
		}

		#region Scene Management
		
		protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
		{
			if (IsInitialized == false)
			{
				return;
			}
			InitializeScene(scene);
		}
		
		protected virtual void OnSceneUnloaded(Scene scene)
		{
			var sceneId = scene.GetNetworkId();
			foreach (var instanceData in new List<NetworkInstanceData>(_instances.Values))
			{
				if (instanceData.Id.SceneId == sceneId)
				{
					if (instanceData.Identity && instanceData.Identity.gameObject.isStatic)
					{
						continue;
					}
					NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Unloading {inst} from spawner data ({type})", instanceData, ToString(instanceData.Type));
					DeleteData(instanceData);
				}
			}
		}

		public void InitializeScene(Scene scene)
		{
			// Find all NetworkId instances in the scene
			var networkInstances = scene.GetRootGameObjects()
				.SelectMany(go => go.GetComponentsInChildren<NetworkIdentity>(true))
				.Where(net => net.IsServerAuthority);

			ulong offset = 0;
			foreach (var networkInstance in networkInstances)
			{
				offset++;
				RegisterStaticInstance(networkInstance);
			}
			_serverObjectIdGenerator.SetSequenceOffset(scene.GetNetworkId(), offset);
		}

		public virtual void Clear(Scene scene)
		{
			foreach (var instance in GetInstancesInScene(scene))
			{
				DespawnInternal(instance.Identity.InstanceId);
			}
			
			// Cleanup pool
			if (_pools.TryGetValue(scene, out GameObjectPool pool))
			{
				pool.Cleanup();
				_pools.Remove(scene);
			}
		}

		#endregion

		#region Instance Management
		
		public Action<NetworkInstanceData> OnSpawn;
		public Action<NetworkInstanceData> OnDespawn;
		
		/// <summary>
		/// <OwnerId, InstanceId[]>
		/// </summary>
		private Dictionary<ushort, HashSet<NetworkInstanceId>> _owners = new Dictionary<ushort, HashSet<NetworkInstanceId>>();
		private Dictionary<NetworkInstanceId, NetworkInstanceData> _instances = new Dictionary<NetworkInstanceId, NetworkInstanceData>();
		
		public bool TryGetIdentity(NetworkInstanceId id, out BaseNetworkInstance identity)
		{
			if (_instances.TryGetValue(id, out var instance))
			{
				identity = instance.Identity;
				return true;
			}
			identity = null;
			return false;
		}

		public bool InstanceExists(NetworkInstanceId id) => _instances.ContainsKey(id);

		public NetworkInstanceData? CreateInstanceData(int? type, BaseNetworkInstance identity, NetworkInstanceId id)
		{
			var instanceId = id;
			var ownerId = instanceId.SessionId;
			if (_instances.TryGetValue(instanceId, out var existingInstance))
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to spawn {inst} ({type}). Behavior already exists with id {id}.\nExisting instance: {exist}", 
					identity.gameObject.name,ToString(type), instanceId, existingInstance.Identity.gameObject.name);
				return null;
			}
			var instance = new NetworkInstanceData(type, identity, instanceId);
			_instances.Add(instanceId, instance);

			if (_owners.TryGetValue(ownerId, out var ids) == false)
			{
				ids = new HashSet<NetworkInstanceId>();
				_owners.Add(ownerId, ids);
			}
			ids.Add(instanceId);

			identity.Initialize(instanceId);
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Created instance {name}[{instanceId}]", identity.name, instanceId);
			
			return instance;
		}

		public IEnumerable<NetworkInstanceData> GetInstancesInScene(Scene scene)
		{
			foreach (var instance in new List<NetworkInstanceData>(_instances.Values))
			{
				if (instance.Identity&& instance.Identity.gameObject.scene == scene)
				{
					yield return instance;
				}
			}
		}
		
		public IEnumerable<BaseNetworkInstance> GetOwnedIdentities(ushort sessionId)
		{
			foreach (var data in GetOwnedInstances(sessionId))
			{
				yield return data.Identity;
			}
		}

		public IEnumerable<NetworkInstanceData> GetOwnedInstances(ushort sessionId)
		{
			if (_owners.TryGetValue(sessionId, out var ids) == false)
			{
				yield break;
			}
			foreach (var id in ids)
			{
				yield return _instances[id];
			}
		}

		public bool TryGetInstance(NetworkInstanceId id, out NetworkInstanceData data)
		{
			return _instances.TryGetValue(id, out data);
		}

		public bool TryGetInstance<T>(NetworkInstanceId id, out T data)
			where T : Component
		{
			if (TryGetIdentity(id, out var identity) && identity.gameObject.TryGetComponent(out data))
			{
				return true;
			}
			data = null;
			return false;
		}

		protected bool DeleteData(NetworkInstanceId id, out NetworkInstanceData data)
		{
			if (_instances.TryGetValue(id, out data))
			{
				return DeleteData(data);
			}
			return false;
		}
		
		private bool DeleteData(NetworkInstanceData instance)
		{
			var id = instance.Identity.InstanceId;
			var owner = id.SessionId;
			if (_instances.Remove(instance.Identity.InstanceId) 
			    && _owners.TryGetValue(owner, out var ownerIds) 
			    && ownerIds.Remove(id))
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Successfully deleted instance {id} | {type}", id, ToString(instance.Type));
				OnDespawn?.Invoke(instance);
				return true;
			}
			return false;
		}

		#endregion

		#region Despawning
		
		public void DespawnLocal(GameObject go)
		{
			if (go.TryGetComponent(out NetworkIdentity identity))
			{
				DespawnLocal(identity);
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not find NetworkIdentity on {go}", go.name);
			}
		}

		public void DespawnLocal(BaseNetworkInstance id)
		{
			if (id == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Despawn - networkId is null");
				return;
			}
			DespawnInternal(id.InstanceId);
		}
		
		public void Despawn(GameObject go)
		{
			if (go.TryGetComponent(out NetworkIdentity identity))
			{
				Despawn(identity);
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not find NetworkIdentity on {go}", go.name);
			}
		}

		public void Despawn(NetworkIdentity identity)
		{
			if (identity.InstanceId == 0)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Identity {name} has a default ID. Not despawning", identity.gameObject.name);
				return;
			}
			
			var id = identity.InstanceId;
			
			var cmd = new DespawnCommand()
			{
				Id = id,
			};

			// Rpc(RpcDespawn, cmd);
			Send(cmd, 0);
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Despawning {type} with Id: {Id}", identity, id);

			DespawnInternal(cmd.Id);
		}
		
		public void Catchup(IEnumerable<SpawnCommand> commands)
		{
			foreach (var command in commands)
			{
				HandleSpawnMessage(command);
			}
		}
		
		protected void DespawnInternal(NetworkInstanceId instanceId)
		{
			if (DeleteData(instanceId, out NetworkInstanceData instance) == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to delete instance {id}. Instance not found", instanceId);
				return;
			}

			if (gameObject == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to despawn {id}. Object is not loaded", instanceId);
				return;
			}

			if (gameObject.scene.isLoaded == false)
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Failed to despawn {name}({id}). Scene is not loaded", gameObject.name, instanceId);
				return;
			}

			var identity = instance.Identity as NetworkIdentity;

			if (identity == null)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Could not find Identity: {id}", instanceId);
				return;
			}
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Despawning: Id:{id} Name: {name}", instanceId, identity.name);
			DestroyIdentity(identity, instance);
		}

		#endregion

		#region Spawning
		
		protected virtual bool Initialize(int? spawnType, NetworkIdentity identity, NetworkInstanceId instanceId)
		{
			var data = CreateInstanceData(spawnType, identity, instanceId);
			if (data == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, $"Failed to create identity {identity} - Could not register");
				Destroy(identity.gameObject);
				return false;
			}
			
			OnSpawn?.Invoke(data.Value);
			
			OnSpawnInternal(spawnType, identity);
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Spawned {inst} with id {id}", ToString(spawnType), instanceId);
			return true;
		}

		private void DestroyIdentity(NetworkIdentity identity, NetworkInstanceData data)
		{
			var instanceType = data.Type;
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

			OnDespawnInternal(data.Id, identity);
		}

		private NetworkIdentity InstantiateIdentity(Scene scene, int spawnType, Vector3 position, Quaternion rotation)
		{
			var parent = SpawnerRegistry<int>.GetParent(spawnType);
			var pool = GetPool(scene);
			var inst = pool.Spawn(spawnType, position, rotation, parent);
			return inst;
		}
		
		public void RegisterStaticInstance(NetworkIdentity identity)
		{
			NetworkInstanceId instanceId = identity.InstanceId;
			{
				// ushort sceneId = identity.gameObject.scene.GetNetworkId();
				// if (identity.IsServerAuthority)
				// {
				// 	instanceId = _serverObjectIdGenerator.GetNextId(sceneId);
				// }
				// else
				// {
				// 	instanceId = _idGenerator.GetNextId(sceneId);
				// }
			}

			Initialize(null, identity, instanceId);
		}
		
		public NetworkIdentity Spawn(Scene scene, int typeValue, Vector3 position, Quaternion rotation)
		{
			var instance = InstantiateIdentity(scene, typeValue, position, rotation);

			ushort sceneId = scene.GetNetworkId();
			NetworkInstanceId id;
			if (instance.IsServerAuthority)
			{
				if (INetworkManager.GetInstance().IsHost() == false)
				{
					throw new Exception($"Trying to spawn a server controlled instance {ToString(typeValue)} as client");
				}
				id = _serverObjectIdGenerator.GetNextId(sceneId);
			}
			else
			{
				id = _idGenerator.GetNextId(sceneId);
			}

			var cmd = new SpawnCommand()
			{
				Id = id,
				SceneId = sceneId,
				SpawnType = typeValue,
				Position = position,
				Rotation = rotation,
			};

			// Send message before initialization (very important)
			// Rpc(RpcSpawn, cmd);

			Send(cmd, 0);
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Spawning {type} with Id: {Id}", ToString(typeValue), id);
			
			Initialize(typeValue, instance, id);

			return instance;
		}
		
		public NetworkIdentity LoadSpawn(Scene scene, int typeValue, Vector3 position, Quaternion rotation)
		{
			var instance = InstantiateIdentity(scene, typeValue, position, rotation);
			
			ushort sceneId = scene.GetNetworkId();
			NetworkInstanceId id;
			if (instance.IsServerAuthority)
			{
				id = _serverObjectIdGenerator.GetNextId(sceneId);
			}
			else
			{
				throw new ArgumentException($"Spawnable {ToString(typeValue)} is not set to server authority and {nameof(LoadSpawn)} was called");
			}
			
			Initialize(typeValue, instance, id);
			
			return instance;
		}

		#endregion

		#region Pools
		
		private GameObjectPool GetPool(Scene scene)
		{
			if (_pools.TryGetValue(scene, out GameObjectPool pool) == false)
			{
				pool = new GameObjectPool(Prefabs);
				_pools[scene] = pool;
			}
			return pool;
		}

		#endregion
		
		#region Virtual Methods

		protected virtual string ToString(int? spawnType)
		{
			return spawnType != null ? spawnType.ToString() : "?";
		}
		
		protected virtual void OnSpawnInternal(int? spawnType, NetworkIdentity identity)
		{
		}
		
		protected virtual void OnDespawnInternal(NetworkInstanceId oldId, NetworkIdentity identity)
		{
		}

		#endregion

		#region RPCs

		private void HandleDespawnMessage(DespawnCommand cmd)
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received despawn command. Id: {Id}", cmd.Id);
			DespawnInternal(cmd.Id);
		}
		
		private void HandleSpawnMessage(SpawnCommand cmd)
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received spawn command. Id: {Id}:{type}", cmd.Id, ToString(cmd.SpawnType));
			var source = cmd.Id.SessionId;
			if (source == SessionId || (source == 0 && INetworkManager.GetInstance().IsHost())) return;

			int spawnable = cmd.SpawnType;
			var scene = SceneManager.GetSceneByBuildIndex(cmd.SceneId);
			var instance = InstantiateIdentity(scene, spawnable, cmd.Position, cmd.Rotation);
			Initialize(spawnable, instance, cmd.Id);
		}

		#endregion
		
		protected override void HandleMessage(INetworkMessage incoming)
		{
			switch (incoming)
			{
				case SpawnCommand spawn:
					HandleSpawnMessage(spawn);
					break;
				case DespawnCommand despawn:
					HandleDespawnMessage(despawn);
					break;
				default:
					base.HandleMessage(incoming);
					break;
			}
		}
	}
}