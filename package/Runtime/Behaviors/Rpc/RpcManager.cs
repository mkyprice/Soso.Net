using System;
using System.Collections.Generic;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors.Rpc
{
    public class RpcManager
    {
	    public readonly bool IsOwner;
	    public readonly ushort SessionId;
	    internal readonly Action<INetworkMessage, ushort> Send;
	    private readonly Dictionary<ushort, RpcTarget> _targets = new Dictionary<ushort, RpcTarget>();

	    public RpcManager(bool isOwner, Action<INetworkMessage, ushort> send)
	    {
		    SessionId = INetworkManager.SessionId;
		    IsOwner = isOwner;
		    Send = send;
	    }

	    public void AddTarget(object target, ushort targetId)
	    {
		    RpcTarget targetRpc = new RpcTarget(target, targetId, this);
		    _targets[targetId] = targetRpc;
	    }
	    public void RemoveTarget(ushort id)
	    {
		    _targets.Remove(id);
	    }
	    public void Clear()
	    {
		    _targets.Clear();
	    }
	    
		public void Process(RpcCall rpc)
		{
			if (_targets.TryGetValue(rpc.TargetId, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", rpc.TargetId, rpc.Method);
				return;
			}
			targetHandler.Process(rpc);
		}

		public void Process(SyncCall sync)
		{
			if (_targets.TryGetValue(sync.TargetId, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "SYNC: {sync} ({id}) target not found", sync.TargetId, sync.SyncId);
				return;
			}
			targetHandler.Process(sync);
		}

		#region RPC Calls

		public void Rpc(Action function, ushort id)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function);
		}
		public void Rpc<T>(Action<T> function, ushort id, T arg0)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0);
		}
		public void Rpc<T0, T1>(Action<T0, T1> function, ushort id, T0 arg0, T1 arg1)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1);
		}
		public void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, ushort id, T0 arg0, T1 arg1, T2 arg2)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2);
		}
		public void Rpc<T0, T1, T2, T3>(Action<T0, T1, T2, T3> function, ushort id, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2, arg3);
		}
		public void Rpc<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> function, ushort id, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (_targets.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2, arg3, arg4);
		}

		#endregion
    }
}