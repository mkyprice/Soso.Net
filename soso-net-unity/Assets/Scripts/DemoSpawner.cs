using Soso.Net.Behaviors;
using UnityEngine;

namespace DefaultNamespace
{
    public class DemoSpawner : TypedNetworkSpawner<SPAWNABLES>
    {
        protected override void OnSpawnInternal(SPAWNABLES? spawnType, NetworkIdentity identity)
        {
            Debug.Log("Spawning " + spawnType.ToString());
            base.OnSpawnInternal(spawnType, identity);
        }
    }
}