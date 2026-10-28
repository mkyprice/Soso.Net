using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using System;
using System.Collections.Generic;
using Soso.Net.Core.Instances;

namespace Soso.Net.Behaviors.Rpc
{
	public class RpcContainer
	{
		public readonly INetworkProcessor Target;
		public readonly ushort TargetId;
		private readonly RpcManager _manager;
		private readonly RpcCache _cache;
		
		public RpcContainer(INetworkProcessor target, RpcManager manager)
		{
			Target = target;
			TargetId = Target.GetLocalId();
			_manager = manager;
			_cache = RpcCache.Get(target.GetType());
			NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Created {name} for {target}({id}). RPCs: {rpcCount}", 
				nameof(RpcContainer), Target.GetType().Name, TargetId, _cache.Count);
		}
		
	    public void Rpc(Action function)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function();
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;

			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			_manager.Send(call, 0);
		}

		public void Rpc<T>(Action<T> function, T arg0)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function(arg0);
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;
			
			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			call.Args = new object[]
			{
				arg0
			};
			_manager.Send(call, 0);
		}

		public void Rpc<T0, T1>(Action<T0, T1> function, T0 arg0, T1 arg1)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function(arg0, arg1);
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;
			
			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			call.Args = new object[]
			{
				arg0,
				arg1
			};
			_manager.Send(call, 0);
		}

		public void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, T0 arg0, T1 arg1, T2 arg2)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function(arg0, arg1, arg2);
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;
			
			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			call.Args = new object[]
			{
				arg0,
				arg1,
				arg2,
			};
			_manager.Send(call, 0);
		}
		
		public void Rpc<T0, T1, T2, T3>(Action<T0, T1, T2, T3> function, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function(arg0, arg1, arg2, arg3);
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;
			
			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			call.Args = new object[]
			{
				arg0,
				arg1,
				arg2,
				arg3
			};
			_manager.Send(call, 0);
		}
		
		public void Rpc<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> function, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			var method = _cache.GetRpcKey(function.Method);
			if (_cache.TryGetValue(method, out var rpc) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} is not registered on target: {target}", function.Method.Name, Target.GetType().Name);
				return;
			}

			if (rpc.Rpc.CallLocal && (rpc.Rpc.CallType == RPC_CALL_TYPE.Client))
			{
				function(arg0, arg1, arg2, arg3, arg4);
			}

			DESTINATION destination;
			if (rpc.Rpc.CallType == RPC_CALL_TYPE.Client)
			{
				destination = DESTINATION.Client;
			}
			else
			{
				destination = DESTINATION.Server;
			}
			
			ushort sourceId = _manager.SessionId;
			
			RpcCall call = new RpcCall()
			{
				Destination = destination,
				SyncTime = rpc.Rpc.SyncTime,
				Time = NetworkTime.Time,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			call.Args = new object[]
			{
				arg0,
				arg1,
				arg2,
				arg3,
				arg4
			};
			_manager.Send(call, 0);
		}
		
		public void Process(RpcCall rpc)
		{
			if (_cache.TryGetValue(rpc.Method, out var handler))
			{
				try
				{
					handler.Method.Invoke(Target, rpc.Args);
				}
				catch (Exception e)
				{
					NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{rpc} ({id}) failed with exception: {e}\n{st}", 
						handler.Method.Name, rpc.Method, e.Message, e.StackTrace);
				}
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "RPC {id} not found on target {target}", rpc.SourceId, Target);
			}
		}
	}
}
