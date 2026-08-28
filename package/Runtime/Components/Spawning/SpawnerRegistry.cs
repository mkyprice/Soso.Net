using System;
using System.Collections.Generic;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    
    public static class SpawnerRegistry<TEnum>
        where TEnum : unmanaged
    {
        public static Dictionary<TEnum, BaseSpawnParent<TEnum>> _parents = new();

        public static Transform GetParent(TEnum spawnType)
        {
            return _parents.TryGetValue(spawnType, out var parent) ? parent.transform : null;
        }

        public static void Register(BaseSpawnParent<TEnum> parent)
        {
            foreach (var value in parent.SpawnTypes)
            {
                _parents[value] = parent;
            }
        }

        public static void Deregister(BaseSpawnParent<TEnum> parent)
        {
            foreach (var value in parent.SpawnTypes)
            {
                if (_parents.TryGetValue(value, out var current) && current == parent)
                {
                    _parents.Remove(value);
                }
            }
        }
    }
}