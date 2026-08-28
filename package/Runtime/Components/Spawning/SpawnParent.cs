using System;
using System.Collections.Generic;
using UnityEngine;

namespace Soso.Net.Components.Spawning
{
    public class SpawnParent : BaseSpawnParent
    {
        [Serializable]
        public struct SpawnRange
        {
            public int Min;
            public int Max;
        }
        [SerializeField] public List<int> SpawnTypes;
        [SerializeField] public List<SpawnRange> SpawnRanges;

        public override IEnumerable<int> GetTypes()
        {
            foreach (var value in SpawnTypes)
            {
                yield return value;
            }

            foreach (var range in SpawnRanges)
            {
                int min = range.Min;
                int max = range.Max;

                for (int i = min; i <= max; i++)
                {
                    yield return i;
                }
            }
        }
    }
}