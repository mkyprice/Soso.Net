using System;
using System.Collections.Generic;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components;
using Soso.Net.Extensions;
using Soso.Net.Logging;
using Soso.Net.Messaging.Basic;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors
{
	public abstract class INetworkReceiver : MonoBehaviour, INetworkPoolable
	{
		[NonSerialized] public BaseNetworkInstance NetId;
		[NonSerialized] public ushort LocalId;
		public bool IsInitialized => _isInitialized;
		private bool _isInitialized = false;

		protected virtual void Start()
		{
		}

		protected virtual void OnDestroy()
		{
			NetId?.Deregister(this);
		}

		public void Initialize(ushort i, BaseNetworkInstance networkIdentity)
		{
			if (_isInitialized)
			{
				NetworkLogger.Warn(CHANNEL.Default, "{name} is already initialized", name);
			}
			LocalId = i;
			NetId = networkIdentity;
			
			_isInitialized = true;
			Initialize();
			NetworkLogger.Debug(CHANNEL.Default, "Initialized {type} {name} with local ID {id}", nameof(INetworkReceiver), name, LocalId);
		}

		protected virtual void Initialize() { }

		#region RPC
		
		public void Rpc(Action function)
			=> NetId.RPC.Rpc(function, LocalId);

		public void Rpc<T>(Action<T> function, T arg0)
			=> NetId.RPC.Rpc(function, LocalId, arg0);

		public void Rpc<T0, T1>(Action<T0, T1> function, T0 arg0, T1 arg1)
			=> NetId.RPC.Rpc(function, LocalId, arg0, arg1);

		public void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, T0 arg0, T1 arg1, T2 arg2)
			=> NetId.RPC.Rpc(function, LocalId, arg0, arg1, arg2);

		public void Rpc<T0, T1, T2, T3>(Action<T0, T1, T2, T3> function, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
			=> NetId.RPC.Rpc(function, LocalId, arg0, arg1, arg2, arg3);

		public void Rpc<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> function, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
			=> NetId.RPC.Rpc(function, LocalId, arg0, arg1, arg2, arg3, arg4);

		#endregion

		#region Poolable

		public virtual void OnSpawn()
		{
			_isInitialized = false;
			NetId = null;
			LocalId = 0;
		}

		public virtual void OnDespawn()
		{
			_isInitialized = false;
			NetId = null;
			LocalId = 0;
		}

		#endregion
	}
}
