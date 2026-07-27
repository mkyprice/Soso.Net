using System;
using System.Collections.Generic;
using System.Reflection;
using Soso.Net.Logging;
using UnityEditor;

namespace Soso.Net.Behaviors.Rpc
{
    internal static class RequestCache
    {
        public static bool TryGetMethod(string path, out ResponseMethod response)
        {
            return  _responseCache.TryGetValue(path, out response);
        }
        
        private static readonly Dictionary<string, ResponseMethod> _responseCache = new Dictionary<string, ResponseMethod>();
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

        public static void Add(object requestHandler)
        {
            var type = requestHandler.GetType();
            var flags = BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in GetAllMethods(type, flags))
            {
                var response = method.GetCustomAttribute<SosoResponse>();
                if (response == null)
                {
                    continue;
                }

                string path = $"{response.Path}({method.GetGenericArguments().Length})";
                _responseCache.Add(path, new ResponseMethod(requestHandler, response, method));
                NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Caching method {m}", path);
            }
        }
    }
}