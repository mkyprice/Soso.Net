using System;
using System.Collections.Generic;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    
    public static class SpawnerRegistry
    {
        public static Dictionary<int, BaseSpawnParent> _parents = new();

        public static Transform GetParent(int spawnType)
        {
            return _parents.TryGetValue(spawnType, out var parent) ? parent.transform : null;
        }

        public static void Register(BaseSpawnParent parent)
        {
            foreach (var type in parent.GetTypes())
            {
                _parents[type] = parent;
            }
        }

        public static void Deregister(BaseSpawnParent parent)
        {
            foreach (var type in parent.GetTypes())
            {
                if (_parents.TryGetValue(type, out var current) && current == parent)
                {
                    _parents.Remove(type);
                }
            }
        }
    }
}