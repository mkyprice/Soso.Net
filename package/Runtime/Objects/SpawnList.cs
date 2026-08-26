using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using UnityEngine;

namespace Soso.Net.Objects
{
    [CreateAssetMenu(fileName = "SosoNet", menuName = "SosoNet/Spawnables", order = 0)]
    [Serializable]
    public class SpawnList : ScriptableObject
    {
        public List<NetworkIdentity> Spawnables;
    }
}