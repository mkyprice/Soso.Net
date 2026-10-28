using System.Collections.Generic;
using Soso.Net.Core.Instances;
using Soso.Net.Core.Sync;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using UnityEngine;

namespace Soso.Net.Behaviors.Rpc
{
    public class SyncContainer
    {
        private readonly INetworkProcessor _target;
        private readonly Dictionary<int, ISosoSync> _syncs;

        public SyncContainer(INetworkProcessor target, RpcManager manager)
        {
            _target = target;
            _syncs = new Dictionary<int, ISosoSync>();
            
            var cache = SyncCache.Get(target.GetType());
            foreach (var sync in cache.GetSosoSyncs(target))
            {
                _syncs[sync.SyncId] = sync;
                sync.Initialize(manager);
            }
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Created {name} for {target}({id}). Syncs: {syncCount}", 
                nameof(SyncContainer), target.GetType().Name, target.GetLocalId(), _syncs.Count);
        }
        
        public void Process(SyncCall sync)
        {
            if (_syncs.TryGetValue(sync.SyncId, out var syncHandler))
            {
                syncHandler.Process(sync);
            }
            else
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Sync ID {id} not found on target {target}", sync.SyncId, _target);
            }
        }
    }
}