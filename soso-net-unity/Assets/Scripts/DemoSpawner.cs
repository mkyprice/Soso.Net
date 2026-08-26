using Soso.Net.Behaviors;
using UnityEngine;

namespace DefaultNamespace
{
    public class DemoSpawner : TypedNetworkSpawner<SPAWNABLES>
    {
        protected override void Spawn(SPAWNABLES? spawnType, NetworkIdentity identity)
        {
            Debug.Log("Spawning " + spawnType.ToString());
            base.Spawn(spawnType, identity);
        }
    }
}