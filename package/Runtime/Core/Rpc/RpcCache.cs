using System;
using System.Collections.Generic;
using System.Reflection;
using Soso.Net.Logging;

namespace Soso.Net.Behaviors.Rpc
{
    public class RpcCache
    {
        private readonly Dictionary<int, RpcMethod> _rpcCalls = new Dictionary<int, RpcMethod>();
        private readonly Dictionary<MethodInfo, int> _methodToKey = new Dictionary<MethodInfo, int>();
        
        public int Count => _rpcCalls.Count;
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
        
        public int GetRpcKey(MethodInfo method)
        {
            if (_methodToKey.TryGetValue(method, out var key) == false)
            {
                key = $"{method.Name}:{method.GetGenericArguments().Length}".GetHashCode();
                _methodToKey[method] = key;
            }
            return key;
        }
        
        private static readonly Dictionary<Type, RpcCache> _cache = new Dictionary<Type, RpcCache>();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static RpcCache Get(Type type)
        {
            if (_cache.TryGetValue(type, out var cache))
            {
                return cache;
            }
            
            cache = BuildAttributeCache(type);
            _cache.Add(type, cache);
            return cache;
        }

        private static RpcCache BuildAttributeCache(Type type)
        {
            var flags = BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly;
            RpcCache cache = new RpcCache();
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
    }
}