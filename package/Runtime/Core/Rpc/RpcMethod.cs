using System.Reflection;

namespace Soso.Net.Behaviors.Rpc
{
    public readonly struct RpcMethod
    {
        public readonly MethodInfo Method;
        public readonly SosoRpc Rpc;

        public RpcMethod(MethodInfo method, SosoRpc rpc)
        {
            Method = method;
            Rpc = rpc;
        }
    }
}