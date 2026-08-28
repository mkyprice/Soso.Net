using System;
using System.Collections.Generic;
using Soso.Net.Extensions;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    public abstract class BaseSpawnParent : MonoBehaviour
    {
        public abstract IEnumerable<int> GetTypes();
        
        private void OnEnable()
        {
            SpawnerRegistry.Register(this);
        }

        private void OnDisable()
        {
            SpawnerRegistry.Deregister(this);
        }
    }
}