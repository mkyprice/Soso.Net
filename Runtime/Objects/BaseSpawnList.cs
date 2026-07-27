using System;
using System.Collections.Generic;
using UnityEngine;

namespace Soso.Net.Objects
{
    public abstract class BaseSpawnList<TEnum> : ScriptableObject
        where TEnum : unmanaged, Enum
    {
        public List<Spawnable<TEnum>> Spawnables;
    }
}