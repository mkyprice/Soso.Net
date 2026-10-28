using System;
using System.Collections.Generic;
using Soso.Net.Core.Instances;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors.Rpc
{
    public class RpcManager
    {
	    public readonly ushort SessionId;
	    private readonly Action<INetworkMessage, ushort> _send;
	    private readonly Dictionary<ushort, RpcContainer> _rpcs = new Dictionary<ushort, RpcContainer>();
	    private readonly Dictionary<ushort, SyncContainer> _syncs = new Dictionary<ushort, SyncContainer>();

	    public RpcManager(Action<INetworkMessage, ushort> send)
	    {
		    SessionId = INetworkManager.SessionId;
		    _send = send;
	    }

	    public void AddTarget(INetworkProcessor target)
	    {
		    var targetId = target.GetLocalId();
		    _rpcs[targetId] = new RpcContainer(target, this);
		    _syncs[targetId] = new SyncContainer(target, this);
	    }
	    
	    public void RemoveTarget(INetworkProcessor target)
	    {
		    var id = target.GetLocalId();
		    _syncs.Remove(id);
		    _rpcs.Remove(id);
	    }
	    
	    public void Clear()
	    {
		    _syncs.Clear();
		    _rpcs.Clear();
	    }
	    
		public void Process(RpcCall rpc)
		{
			if (_rpcs.TryGetValue(rpc.TargetId, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: Target({rpc}) not found. RPC method: {method}", rpc.TargetId, rpc.Method);
				return;
			}
			targetHandler.Process(rpc);
		}

		public void Process(SyncCall sync)
		{
			if (_syncs.TryGetValue(sync.TargetId, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "SYNC: Target({rpc}) not found. Sync ID: {sync}", sync.TargetId, sync.SyncId);
				return;
			}
			targetHandler.Process(sync);
		}

		public void Send(INetworkMessage message, ushort channel)
		{
			_send(message, channel);
		}

		#region RPC Calls

		public void Rpc(Action function, ushort id)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function);
		}
		public void Rpc<T>(Action<T> function, ushort id, T arg0)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0);
		}
		public void Rpc<T0, T1>(Action<T0, T1> function, ushort id, T0 arg0, T1 arg1)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1);
		}
		public void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, ushort id, T0 arg0, T1 arg1, T2 arg2)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2);
		}
		public void Rpc<T0, T1, T2, T3>(Action<T0, T1, T2, T3> function, ushort id, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2, arg3);
		}
		public void Rpc<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> function, ushort id, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (_rpcs.TryGetValue(id, out var targetHandler) == false)
			{
				NetworkLogger.Error(CHANNEL.Default, "RPC: {rpc} ({id}) target not found", id, function.Method.Name);
				return;
			}
			targetHandler.Rpc(function, arg0, arg1, arg2, arg3, arg4);
		}

		#endregion
    }
}