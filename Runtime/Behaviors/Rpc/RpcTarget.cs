using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using System;
using System.Collections.Generic;

namespace Soso.Net.Behaviors.Rpc
{
	public readonly struct RpcTarget
	{
		public readonly object Target;
		public readonly ushort TargetId;
		private readonly RpcManager _manager;
		private readonly RpcContainer _container;
		private readonly Dictionary<int, ISosoSync> _syncs;
		    
		public RpcTarget(object target, ushort targetId, RpcManager manager)
		{
			Target = target;
			TargetId = targetId;
			_manager = manager;
			_container = RpcContainer.Get(target.GetType());
			_syncs = new Dictionary<int, ISosoSync>();
			int syncId = 0;
			foreach (var sync in _container.GetSosoSyncs(Target))
			{
				_syncs[syncId] = sync;
				sync.Initialize(this, syncId);
				syncId++;
			}
		}

		public void SetSync<T>(SosoSync<T> sync, T value) 
			where T : unmanaged
		{
			if (_manager.IsOwner == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Cannot set value when not owner. Value: {value}", value);
				return;
			}
			
			sync.SetInternal(value);
			
			SyncCall call = new SyncCall()
			{
				Destination = DESTINATION.Client,
				SyncTime = sync.SyncTime,
				Time = NetworkTime.LocalTime,
				
				TargetId = TargetId,
				SyncId = sync.Id,
				Arg = value,
			};
			_manager.Send(call, 0);
		}
		
	    public void Rpc(Action function)
		{
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
			};
			call.TargetId = TargetId;
			call.SourceId = sourceId;
			call.Method = method;
			_manager.Send(call, 0);
		}

		public void Rpc<T>(Action<T> function, T arg0)
		{
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
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
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
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
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
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
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
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
			var method = _container.GetRpcKey(function.Method);
			if (_container.TryGetValue(method, out var rpc) == false)
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
				Time = NetworkTime.LocalTime,
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
			if (_container.TryGetValue(rpc.Method, out var handler))
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
		}
		public void Process(SyncCall sync)
		{
			if (_syncs.TryGetValue(sync.SyncId, out var syncHandler))
			{
				syncHandler.SetInternal(sync.Arg);
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not set {v} for sync {id}", sync.Arg, sync.SyncId);
			}
		}
	}
}
