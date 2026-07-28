using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Soso.Net.Logging;

namespace Soso.Net.Behaviors.Rpc
{
    public class RpcContainer
    {
        private readonly Dictionary<int, RpcMethod> _rpcCalls = new Dictionary<int, RpcMethod>();
        private readonly Dictionary<MethodInfo, int> _methodToKey = new Dictionary<MethodInfo, int>();
        private readonly List<FieldInfo> _sosoSyncs = new List<FieldInfo>();
        
        public bool HasMethod(int methodId) => _rpcCalls.ContainsKey(methodId);
        public bool TryGetValue(int methodId, out RpcMethod rpc) => _rpcCalls.TryGetValue(methodId, out rpc);

        private void Add(MethodInfo method, SosoRpc rpc)
        {
            int key = GetRpcKey(method);
            if (_rpcCalls.TryAdd(key, new RpcMethod(method, rpc)) == false)
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to add RPC method {method}. Key has already been added",  method.Name);
            }
        }

        private void AddSosoSync(FieldInfo prop)
        {
            _sosoSyncs.Add(prop);
        }
		
        public int GetRpcKey(MethodInfo method)
        {
            if (_methodToKey.TryGetValue(method, out var key) == false)
            {
                key = $"{method.Name}:{method.GetGenericArguments().Length}".GetHashCode();
                _methodToKey[method] = key;
            }
            return key;
        }

        internal IEnumerable<ISosoSync> GetSosoSyncs(object target)
        {
            foreach (var prop in _sosoSyncs.OrderBy(p => p.Name))
            {
                if (prop.GetValue(target) is ISosoSync sync)
                {
                    yield return sync;
                }
            }
        }
        
        private static readonly Dictionary<Type, RpcContainer> _cache = new Dictionary<Type, RpcContainer>();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static RpcContainer Get(Type type)
        {
            if (_cache.TryGetValue(type, out var cache))
            {
                return cache;
            }
            
            cache = BuildAttributeCache(type);
            _cache.Add(type, cache);
            return cache;
        }

        private static RpcContainer BuildAttributeCache(Type type)
        {
            var flags = BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly;
            RpcContainer cache = new RpcContainer();
            foreach (MethodInfo method in GetAllMethods(type, flags))
            {
                var rpc = method.GetCustomAttribute<SosoRpc>();
                if (rpc == null)
                {
                    continue;
                }
                cache.Add(method, rpc);
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Caching method {m}", method.Name);
            }

            foreach (var syncProp in GetAllMatchingFields(type, typeof(SosoSync<>), flags))
            {
                cache.AddSosoSync(syncProp);
            }
            
            return cache;
        }

        private static IEnumerable<MethodInfo> GetAllMethods(Type type, BindingFlags flags)
        {
            while (type != null)
            {
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    yield return method;
                }
                type = type.BaseType;
            }
        }

        private static IEnumerable<FieldInfo> GetAllMatchingFields(Type type, Type genericType, BindingFlags flags)
        {
            while (type != null)
            {
                foreach (var method in type.GetFields())
                {
                    if (method.FieldType.IsGenericType && method.FieldType.GetGenericTypeDefinition() == genericType)
                    {
                        yield return method;
                    }
                }
                type = type.BaseType;
            }
        }
    }
}