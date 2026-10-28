using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Soso.Net.Core.Instances;
using Soso.Net.Core.Sync;
using Soso.Net.Logging;

namespace Soso.Net.Behaviors.Rpc
{
    public class SyncCache
    {
        private readonly List<FieldInfo> _sosoSyncs = new List<FieldInfo>();
        public int Count => _sosoSyncs.Count;

        private void AddSosoSync(FieldInfo prop)
        {
            _sosoSyncs.Add(prop);
        }

        internal IEnumerable<ISosoSync> GetSosoSyncs(INetworkProcessor target)
        {
            foreach (var prop in _sosoSyncs.OrderBy(p => p.Name))
            {
                if (prop.GetValue(target) is ISosoSync sync)
                {
                    sync.Setup(prop.Name, target);
                    yield return sync;
                }
            }
        }

        #region Static Interface
        
        private static readonly Dictionary<Type, SyncCache> _cache = new Dictionary<Type, SyncCache>();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static SyncCache Get(Type type)
        {
            if (_cache.TryGetValue(type, out var cache))
            {
                return cache;
            }
            
            cache = BuildAttributeCache(type);
            _cache.Add(type, cache);
            return cache;
        }

        private static SyncCache BuildAttributeCache(Type type)
        {
            var flags = BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly;
            SyncCache cache = new SyncCache();

            foreach (var syncProp in GetAllMatchingFields(type, typeof(SosoSync<>), flags))
            {
                cache.AddSosoSync(syncProp);
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Caching SosoSync {m}", syncProp.Name);
            }
            
            return cache;
        }

        private static IEnumerable<FieldInfo> GetAllMatchingFields(Type type, Type genericType, BindingFlags flags)
        {
            while (type != null)
            {
                foreach (var method in type.GetFields(flags))
                {
                    if (method.FieldType.IsGenericType && method.FieldType.GetGenericTypeDefinition() == genericType)
                    {
                        yield return method;
                    }
                }
                type = type.BaseType;
            }
        }

        #endregion
    }
}