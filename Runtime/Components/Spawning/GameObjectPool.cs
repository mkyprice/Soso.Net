using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Logging;
using Soso.Net.Objects;
using Soso.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Soso.Net.Components.Spawning
{
    [Serializable]
    public class GameObjectPool<TEnum>
        where TEnum : unmanaged, Enum
    {
        private readonly BaseSpawnList<TEnum> _prefabs;
        
        private readonly Dictionary<TEnum, Queue<NetworkIdentity>> _pool = new Dictionary<TEnum, Queue<NetworkIdentity>>();

        public GameObjectPool(BaseSpawnList<TEnum> prefabs)
        {
            _prefabs = prefabs;
        }

        public Spawnable<TEnum> GetSpawnable(TEnum spawnType)
        {
            var prefab = _prefabs.Spawnables.FirstOrDefault(p => p.TypeId.Equals(spawnType));
            return prefab;
        }

        public void Initialize(TEnum spawnType, int startPoolSize)
        {
            var prefab = GetSpawnable(spawnType);
            if (prefab.Prefab == null)
            {
                throw new Exception($"Spawnable {spawnType} not found");
            }
            
            var queue = GetPool(spawnType);
            for (int i = 0; i < startPoolSize; i++)
            {
                var instance = Object.Instantiate(prefab.Prefab);
                
                var poolables = instance.gameObject.GetComponentsInChildren<INetworkPoolable>(true);
                foreach (var poolable in poolables)
                {
                    poolable.OnDespawn();
                }
                
                instance.gameObject.SetActive(false);
                queue.Enqueue(instance);
            }
        }

        public NetworkIdentity Spawn(TEnum spawnType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            NetworkIdentity instance = null;
            var queue = GetPool(spawnType);
            if (queue.Count > 0)
            {
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Spawning {inst} from pool", spawnType);
                instance = queue.Dequeue();
                if (instance)
                {
                    instance.transform.SetParent(parent);
                    instance.transform.SetPositionAndRotation(position, rotation);
                    instance.gameObject.SetActive(true);
                }
                else
                {
                    NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Instance {type} was destroyed in pool", spawnType);
                    instance = null;
                }
            }
            if (instance is null)
            {
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Spawning {inst} from initialization", spawnType);
                var prefab = GetSpawnable(spawnType);
                if (prefab.Prefab == null)
                {
                    throw new Exception($"Spawnable {spawnType} not found");
                }
                instance = Object.Instantiate(prefab.Prefab, position, rotation, parent);
            }
            
            instance.OnSpawn();
            var poolables = instance.gameObject.GetComponentsInChildren<INetworkPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnSpawn();
            }

            return instance;
        }

        public void Return(NetworkIdentity instance, TEnum spawnType)
        {
            if (instance?.gameObject?.scene.isLoaded == false)
            {
                NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Not returning {inst} to pool. Scene is not loaded", spawnType);
                return;
            }
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Returning {inst} to pool", instance.gameObject.name);
            var queue = GetPool(spawnType);
            instance.gameObject.SetActive(false);
            
            var poolables = instance.gameObject.GetComponentsInChildren<INetworkPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnDespawn();
            }
            
            queue.Enqueue(instance);
        }

        private Queue<NetworkIdentity> GetPool(TEnum spawnType)
        {
            if (_pool.TryGetValue(spawnType, out var queue) == false)
            {
                queue = new Queue<NetworkIdentity>();
                _pool.Add(spawnType, queue);
            }
            return queue;
        }

        public bool HasType(TEnum spawnType)
        {
            return _pool.ContainsKey(spawnType);
        }

        public void Cleanup()
        {
            foreach (var poolQueue in _pool.Values)
            {
                while (poolQueue.Count > 0)
                {
                    var go = poolQueue.Dequeue();
                    if (go)
                    {
                        Object.Destroy(go.gameObject);
                    }
                }
            }
            _pool.Clear();
        }
    }
}