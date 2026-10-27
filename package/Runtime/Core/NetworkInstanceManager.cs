using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Objects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Soso.Net.Core
{
	[Serializable]
    public class NetworkInstanceManager
    {
		/// <summary>
		/// <OwnerId, InstanceId[]>
		/// </summary>
		private readonly Dictionary<ushort, HashSet<NetworkInstanceId>> _owners = new Dictionary<ushort, HashSet<NetworkInstanceId>>();
		private readonly Dictionary<NetworkInstanceId, NetworkInstance> _instances = new Dictionary<NetworkInstanceId, NetworkInstance>();
		private object _instanceLock = new object();
		

		public void Initialize()
		{
			
		}

		public void Reset()
		{
			// TODO: Ensure everything is despawned
			_instances.Clear();
			_owners.Clear();
		}
		
		public bool TryGetIdentity(NetworkInstanceId id, out INetworkIdentity identity)
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

		public NetworkInstance GetOrCreateInstance(NetworkInstanceId id)
		{
			lock (_instanceLock)
			{
				var instanceId = id;
				var ownerId = instanceId.SessionId;
				if (_instances.TryGetValue(instanceId, out var instance))
				{
					if (instance.Id != id)
					{
						NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "NetworkInstance {id} exists but its ID is set to {bad}", id, instance.Id);
					}
					return instance;
				}
				instance = new NetworkInstance(instanceId);
				_instances.Add(instanceId, instance);

				if (_owners.TryGetValue(ownerId, out var ids) == false)
				{
					ids = new HashSet<NetworkInstanceId>();
					_owners.Add(ownerId, ids);
				}
				ids.Add(instanceId);
				
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Created instance [{instanceId}]", instanceId);
				
				return instance;
			}
		}

		public bool SetInstanceIdentity(int? type, INetworkIdentity identity, NetworkInstanceId id)
		{
			var instanceData = GetOrCreateInstance(id);
			
			if (instanceData.Identity == identity)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Instance identity {id} is already set to identity {inst}", id, identity);
				return true;
			}

			if (instanceData.Identity != null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Identity already exists for id {id}", id);
				return false;
			}

			instanceData.SetInstance(identity, type);

			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Set instance {name}[{instanceId}]", identity.GetName(), id);

			return true;
		}

		public IEnumerable<NetworkInstance> GetInstancesInScene(Scene scene)
		{
			var sceneId = scene.GetNetworkId();
			foreach (var instance in new List<NetworkInstance>(_instances.Values))
			{
				if (instance.Id.SceneId == sceneId)
				{
					yield return instance;
				}
			}
		}
		
		public IEnumerable<INetworkIdentity> GetOwnedIdentities(ushort sessionId)
		{
			foreach (var data in GetOwnedInstances(sessionId))
			{
				if (data.Identity != null)
				{
					yield return data.Identity;
				}
			}
		}
		public IEnumerable<NetworkInstance> GetOwnedInstances(ushort sessionId)
		{
			lock (_instanceLock)
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
		}

		public bool TryGetInstance(NetworkInstanceId id, out NetworkInstance data)
		{
			return _instances.TryGetValue(id, out data);
		}

		public bool TryGetInstance<T>(NetworkInstanceId id, out T data)
			where T : Component
		{
			if (TryGetIdentity(id, out var identity) && identity is BaseNetworkInstance goId && goId.gameObject.TryGetComponent(out data))
			{
				return true;
			}
			data = null;
			return false;
		}

		public bool DeleteData(NetworkInstanceId id, out NetworkInstance data)
		{
			lock (_instanceLock)
			{
				var owner = id.SessionId;
				bool removeInst = _instances.Remove(id, out data);
				bool removedOwner = _owners.TryGetValue(owner, out var ownerIds) && ownerIds.Remove(id);
				if (removeInst && removedOwner)
				{
					NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Successfully deleted instance {id} | {type}", id, ToString(data.Type));
					return true;
				}

				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to delete instance {id}. Inst found: {id} Owner delete: {od}", id, removeInst, removedOwner);
				return false;
			}
		}
		
		protected virtual string ToString(int? spawnType)
		{
			return spawnType != null ? spawnType.ToString() : "?";
		}
    }
}