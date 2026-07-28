using System;

namespace Soso.Net.Behaviors.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SosoResponse : Attribute
    {
        public string Path;

        public SosoResponse(string path)
        {
            Path = path;
        }
    }
}