using System;
using Soso.Net.Models;

namespace Soso.Net.Behaviors.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SosoRpc : Attribute
    {
        public bool SyncTime = true;
        public bool CallLocal = true;
        public RPC_CALL_TYPE CallType = RPC_CALL_TYPE.Client;
        public SOSO_SEND_TYPE SendType = SOSO_SEND_TYPE.Reliable;

        public SosoRpc()
        {
        }

        public SosoRpc(RPC_CALL_TYPE callType = RPC_CALL_TYPE.Client, bool syncTime = true, bool callLocal = true, SOSO_SEND_TYPE sendType = SOSO_SEND_TYPE.Reliable)
        {
            CallType = callType;
            SyncTime = syncTime;
            SendType = sendType;
            CallLocal = callLocal;
        }
    }
}