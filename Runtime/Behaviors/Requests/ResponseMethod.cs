using System.Reflection;

namespace Soso.Net.Behaviors.Rpc
{
    public readonly struct ResponseMethod
    {
        public readonly object Target;
        public readonly SosoResponse Response;
        public readonly MethodInfo MethodInfo;

        public ResponseMethod(object target, SosoResponse response, MethodInfo methodInfo)
        {
            Target = target;
            Response = response;
            MethodInfo = methodInfo;
        }
    }
}