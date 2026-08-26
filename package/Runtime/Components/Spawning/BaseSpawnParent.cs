using System;
using System.Collections.Generic;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    public class BaseSpawnParent<TEnum> : MonoBehaviour
        where TEnum : unmanaged
    {
        [SerializeField] public List<TEnum> SpawnTypes;

        private void OnEnable()
        {
            SpawnerRegistry<TEnum>.Register(this);
        }

        private void OnDisable()
        {
            SpawnerRegistry<TEnum>.Deregister(this);
        }
    }
}