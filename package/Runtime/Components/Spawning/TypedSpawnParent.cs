using System;
using System.Collections.Generic;
using Soso.Net.Extensions;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    public class TypedSpawnParent<TEnum> : BaseSpawnParent
        where TEnum : unmanaged, Enum
    {
        [Serializable]
        public struct SpawnRange
        {
            public TEnum Min;
            public TEnum Max;
        }
        [SerializeField] public List<TEnum> SpawnTypes;
        [SerializeField] public List<SpawnRange> SpawnRanges;
        
        public override IEnumerable<int> GetTypes()
        {
            foreach (var value in SpawnTypes)
            {
                int type = (int)value.ToValue();
                yield return type;
            }
            
            foreach (var range in SpawnRanges)
            {
                int min = (int)range.Min.ToValue();
                int max = (int)range.Max.ToValue();

                for (int i = min; i <= max; i++)
                {
                    yield return i;
                }
            }
        }
    }
}