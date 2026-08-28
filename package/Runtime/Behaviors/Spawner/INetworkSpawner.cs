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
	public class INetworkSpawner : BaseNetworkInstance
	{
		[SerializeField] public SpawnList Prefabs;
		
		public override bool IsOwner => true;
		protected ushort SessionId { get; private set; }
		public new NetworkController Network { get; private set; }
		
		private NetworkIdGenerator _idGenerator;
		private NetworkIdGenerator _serverObjectIdGenerator;
		private Dictionary<Scene, GameObjectPool> _pools = new Dictionary<Scene, GameObjectPool>();

		private void Start()
		{
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
				DestroyInstance(instance.Identity.InstanceId);
			}
			
			// Cleanup pool
			if (_pools.TryGetValue(scene, out GameObjectPool pool))
			{
				pool.Cleanup();
				_pools.Remove(scene);
			}
		}
		
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

		#endregion

		#region Instance Management
		
		public Action<NetworkInstanceData> OnInstanceSpawned;
		public Action<NetworkInstanceData> OnInstanceDespawned;
		
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
				OnInstanceDespawned?.Invoke(instance);
				return true;
			}
			return false;
		}

		#endregion

		#region Despawning
		
		/// <summary>
		/// Despawn identity across all clients
		/// </summary>
		/// <param name="go"></param>
		public void Despawn(GameObject go)
		{
			if (go.TryGetComponent(out NetworkIdentity instance))
			{
				Despawn(instance);
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not find NetworkIdentity on {go}", go.name);
			}
		}

		/// <summary>
		/// Despawn identity across all clients
		/// </summary>
		/// <param name="instance"></param>
		public void Despawn(NetworkIdentity instance)
		{
			if (instance.InstanceId == 0)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Identity {name} has a default ID. Not despawning", instance.gameObject.name);
				return;
			}
			
			var id = instance.InstanceId;
			
			var cmd = new DespawnCommand()
			{
				Id = id,
			};

			Send(cmd, 0);
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Despawning {name} with Id: {Id}", instance.gameObject.name, id);

			DestroyInstance(cmd.Id);
		}
		
		/// <summary>
		/// Despawns instance but does not send despawn command
		/// </summary>
		/// <param name="go"></param>
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

		/// <summary>
		/// Despawns instance but does not send despawn command
		/// </summary>
		/// <param name="id"></param>
		public void DespawnLocal(NetworkIdentity id)
		{
			if (id == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Despawn - networkId is null");
				return;
			}
			DestroyInstance(id.InstanceId);
		}
		
		/// <summary>
		/// Internally destroy or return an instance to the pool
		/// </summary>
		/// <param name="instanceId"></param>
		private void DestroyInstance(NetworkInstanceId instanceId)
		{
			if (DeleteData(instanceId, out NetworkInstanceData instance) == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to delete instance {id}. Instance not found", instanceId);
				return;
			}
			
			var instanceObj = instance.Identity;
			
			if (instanceObj == false || instanceObj.gameObject == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to despawn {id}. Object is not loaded", instanceId);
				return;
			}

			var scene = instanceObj.gameObject.scene;
			if (scene.isLoaded == false)
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Failed to despawn {name}({id}). Scene is not loaded", instanceObj.name, instanceId);
				return;
			}
			
			var identity = (NetworkIdentity)instanceObj;
			if (identity == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Could not find Identity: {id}", instanceId);
				return;
			}
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Despawning: Id:{id} Name: {name}", instanceId, identity.name);
			
			// Return to pool
			var instanceType = instance.Type;
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

			OnDespawnInternal(instance.Id, identity);
		}

		#endregion

		#region Spawning
		
		public void Catchup(IEnumerable<SpawnCommand> commands)
		{
			foreach (var command in commands)
			{
				HandleSpawnMessage(command);
			}
		}
		
		public void RegisterStaticInstance(NetworkIdentity identity)
		{
			NetworkInstanceId instanceId = identity.InstanceId;
			InitializeIdentity(null, identity, instanceId);
		}
		
		public NetworkIdentity Spawn(Scene scene, int typeValue, Vector3 position, Quaternion rotation)
		{
			var instance = InstantiateIdentity(scene, typeValue, position, rotation);

			ushort sceneId = scene.GetNetworkId();
			NetworkInstanceId id;
			if (instance.IsServerAuthority && instance.IsClientAuthority == false)
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
				SpawnType = typeValue,
				Position = position,
				Rotation = rotation,
			};

			// Send message before initialization (very important)
			// Rpc(RpcSpawn, cmd);

			Send(cmd, 0);
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Spawning {type} with Id: {Id}", ToString(typeValue), id);
			
			InitializeIdentity(typeValue, instance, id);

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
			
			InitializeIdentity(typeValue, instance, id);
			
			return instance;
		}
		
		private bool InitializeIdentity(int? spawnType, NetworkIdentity identity, NetworkInstanceId instanceId)
		{
			var data = CreateInstanceData(spawnType, identity, instanceId);
			if (data == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, $"Failed to create identity {identity} - Could not register");
				Destroy(identity.gameObject);
				return false;
			}
			
			OnInstanceSpawned?.Invoke(data.Value);
			
			OnSpawnInternal(spawnType, identity);
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Spawned {inst} with id {id}", ToString(spawnType), instanceId);
			return true;
		}

		private NetworkIdentity InstantiateIdentity(Scene scene, int spawnType, Vector3 position, Quaternion rotation)
		{
			var parent = SpawnerRegistry.GetParent(spawnType);
			var pool = GetPool(scene);
			var inst = pool.Spawn(spawnType, position, rotation, parent);
			return inst;
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

		#region Network Messages
		
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
		
		private void HandleSpawnMessage(SpawnCommand cmd)
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received spawn command. Id: {Id}:{type}", cmd.Id, ToString(cmd.SpawnType));
			var source = cmd.Id.SessionId;
			if (source == SessionId || (source == 0 && INetworkManager.GetInstance().IsHost())) return;

			int spawnable = cmd.SpawnType;
			var scene = SceneManager.GetSceneByBuildIndex(cmd.Id.SceneId);
			var instance = InstantiateIdentity(scene, spawnable, cmd.Position, cmd.Rotation);
			InitializeIdentity(spawnable, instance, cmd.Id);
		}

		private void HandleDespawnMessage(DespawnCommand cmd)
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received despawn command. Id: {Id}", cmd.Id);
			DestroyInstance(cmd.Id);
		}
		
		#endregion
	}
}