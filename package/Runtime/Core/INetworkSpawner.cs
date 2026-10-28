using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components.Spawning;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Net.Objects;
using Soso.Net.Utils;
using Soso.Utils.Concurrency;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Behaviors
{
	public class INetworkSpawner : BaseNetworkIdentity
	{
		[SerializeField] public SpawnList Prefabs;
		
		public override bool IsAuthority() => true;
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
			_sceneInitialization.Clear();
			INetworkManager.GetInstanceManager().DeleteData(NetworkInstanceId.Spawner, out _);
			ResetInstance();
		}

		public void InitializeSpawner()
		{
			Network = INetworkManager.GetInstance().Network;
			SessionId = INetworkManager.GetInstance().Session.Session?.SessionId ?? SessionInfo.SERVER_SESSION_ID;
			
			_idGenerator = new NetworkIdGenerator(SessionId);
			_serverObjectIdGenerator = new NetworkIdGenerator(SessionInfo.SERVER_SESSION_ID);

			if (INetworkManager.GetInstanceManager().InstanceExists(NetworkInstanceId.Spawner) == false
				&& INetworkManager.GetInstanceManager().SetInstanceIdentity(null, this, NetworkInstanceId.Spawner) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not register {name}", name);
			}
			
			// Initialize the starting scene
			InitializeScene(gameObject.scene);
		}

		public virtual void Shutdown()
		{
		}

		#region Scene Management

		private Dictionary<ushort, bool> _sceneInitialization = new Dictionary<ushort, bool>();
		
		public void InitializeScene(Scene scene)
		{
			if (IsSceneInitialized(scene))
			{
				return;
			}
			// Find all NetworkId instances in the scene
			var networkInstances = scene.GetRootGameObjects()
				.SelectMany(go => go.GetComponentsInChildren<NetworkIdentity>(true))
				.Where(net => net.BakedId != 0);

			ulong offset = 0;
			foreach (var networkInstance in networkInstances)
			{
				offset++;
				RegisterStaticInstance(networkInstance);
			}
			_serverObjectIdGenerator.SetSequenceOffset(scene.GetNetworkId(), offset);
			_sceneInitialization[scene.GetNetworkId()] = true;
		}

		public bool IsSceneInitialized(Scene scene)
		{
			return _sceneInitialization.TryGetValue(scene.GetNetworkId(), out var isInitialized) && isInitialized;
		}

		public virtual void Clear(Scene scene)
		{
			_sceneInitialization.Remove(scene.GetNetworkId());
			
			foreach (var instance in INetworkManager.GetInstanceManager().GetInstancesInScene(scene))
			{
				DestroyInstance(instance.Id);
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
			Clear(scene);
		}

		#endregion

		#region Despawning
		
		/// <summary>
		/// Despawn identity across all clients
		/// </summary>
		/// <param name="go"></param>
		/// <param name="syncTime"></param>
		public void Despawn(GameObject go, bool syncTime = true)
		{
			if (go.TryGetComponent(out NetworkIdentity instance))
			{
				Despawn(instance, syncTime);
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
		/// <param name="syncTime"></param>
		public void Despawn(NetworkIdentity instance, bool syncTime = true)
		{
			var id = instance.InstanceId;
			
			if (instance.InstanceId == 0)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Identity {name} has a default ID. Not despawning", instance);
				return;
			}
			
			DestroyInstance(id);
			
			var cmd = new DespawnCommand()
			{
				Id = id,
				SourceId = SessionId,
				SyncTime = syncTime,
			};
			Send(cmd, 0);
			
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Despawning {name} with Id: {Id}", instance, id);
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
			if (INetworkManager.GetInstanceManager().DeleteData(instanceId, out var instance) == false || instance.Identity is BaseNetworkIdentity instanceObj == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to delete instance {id}. Instance not found", instanceId);
				return;
			}
			
			if (instanceObj == false || instanceObj.gameObject == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Failed to despawn {id}. Object is not loaded", instanceId);
				return;
			}

			var scene = instanceObj.gameObject.scene;
			if (scene.isLoaded == false)
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Failed to despawn {name}({id}). Scene is not loaded", instanceObj.GetName(), instanceId);
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
			NetworkInstanceId instanceId = identity.BakedId;
			InitializeIdentity(null, identity, instanceId);
		}

		public NetworkIdentity Spawn(Scene scene, GameObject prefab, Vector3 position, Quaternion rotation)
		{
			var pool = GetPool(scene);
			int typeValue = pool.FindType(prefab);
			if (typeValue < 0)
			{
				throw new ArgumentException($"Cannot find spawnable {prefab.name} to {nameof(Spawn)}");
			}
			return Spawn(scene, typeValue, position, rotation);
		}
		
		public NetworkIdentity Spawn(Scene scene, int typeValue, Vector3 position, Quaternion rotation, bool syncTime = true)
		{
			var instance = InstantiateIdentity(scene, typeValue, position, rotation);

			ushort sceneId = scene.GetNetworkId();
			NetworkInstanceId id;
			if (instance.IsServerAuthority && instance.IsClientAuthority == false)
			{
				if (INetworkManager.GetInstance().IsServer == false)
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
				SyncTime = syncTime,
			};

			// Send message before initialization (very important)
			// Rpc(RpcSpawn, cmd);

			Send(cmd, 0);
			
			InitializeIdentity(typeValue, instance, id);

			return instance;
		}

		public NetworkIdentity LoadSpawn(Scene scene, GameObject prefab, Vector3 position, Quaternion rotation)
		{
			var pool = GetPool(scene);
			int typeValue = pool.FindType(prefab);
			if (typeValue < 0)
			{
				throw new ArgumentException($"Cannot find spawnable {prefab.name} to {nameof(LoadSpawn)}");
			}
			return LoadSpawn(scene, typeValue, position, rotation);
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
			if (INetworkManager.GetInstanceManager().SetInstanceIdentity(spawnType, identity, instanceId) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, $"Failed to create identity {identity} - Could not register");
				Destroy(identity.gameObject);
				return false;
			}
			
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
		
		public override bool HandleMessage(INetworkMessage incoming)
		{
			switch (incoming)
			{
				case SpawnCommand spawn:
					return HandleSpawnMessage(spawn);
				case DespawnCommand despawn:
					return HandleDespawnMessage(despawn);
				default:
					return base.HandleMessage(incoming);
			}
		}
		
		private bool HandleSpawnMessage(SpawnCommand cmd)
		{
			if (_sceneInitialization.TryGetValue(cmd.Id.SceneId, out bool isSceneReady) == false || isSceneReady == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Client received spawn command. Id: {Id}:{type} but scene {scene} is not ready", cmd.Id, ToString(cmd.SpawnType), cmd.Id.SceneId);
				return false;
			}
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received spawn command. Id: {Id}:{type}", cmd.Id, ToString(cmd.SpawnType));
			var source = cmd.Id.SessionId;
			if (source == SessionId || (source == 0 && INetworkManager.GetInstance().IsServer)) return true;

			int spawnable = cmd.SpawnType;
			var scene = SceneManager.GetSceneByBuildIndex(cmd.Id.SceneId);
			var instance = InstantiateIdentity(scene, spawnable, cmd.Position, cmd.Rotation);
			InitializeIdentity(spawnable, instance, cmd.Id);
			return true;
		}

		private bool HandleDespawnMessage(DespawnCommand cmd)
		{
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Client received despawn command. Id: {Id}", cmd.Id);
			
			// We sent it and have already deleted it
			var source = cmd.SourceId;
			if (source == SessionId) return true;
			
			DestroyInstance(cmd.Id);
			return true;
		}
		
		#endregion
	}
}