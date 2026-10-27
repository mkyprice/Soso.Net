using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Utils.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;
using Soso.Net.Objects;
using UnityEngine;

namespace Soso.Net.Behaviors
{
	public abstract class BaseNetworkInstance : MonoBehaviour, INetworkPoolable, INetworkIdentity
	{
		[Header("DEBUG DO NOT MODIFY")]
		[SerializeField] private NetworkInstance _instance;
		public NetworkInstance Instance => _instance;
		public NetworkInstanceId InstanceId => Instance?.Id ?? NetworkInstanceId.Invalid;
		public RemoteInfo RemoteInfo => Instance.RemoteInfo;

		public abstract bool IsAuthority { get; }
		public virtual ushort AuthorityId => InstanceId.SessionId;
		public ulong Sequence => InstanceId.SequenceNumber;
		public double RemoteTime => RemoteInfo.RemoteTime;
		public double Ping => RemoteInfo.Ping;
		public bool IsServer
		{
			get
			{
				return Network == null || Network.IsHost || Network.Network.IsOffline;
			}
		}
		
		
		private static readonly ushort MY_RPC_RECEIVER_ID = ushort.MaxValue;
		
		public bool IsInitialized => _isInitialized;

		public NetworkController Network => _networkController;

		public RpcManager RPC => _rpc;
		
		private RpcManager _rpc;
		private NetworkController _networkController;
		private bool _isInitialized = false;
		private CancellationTokenSource _cancellationTokenSource;

		public string GetName()
		{
			return name;
		}

		public virtual void Initialize(NetworkInstance instance)
		{
			if (_isInitialized)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} was already initialized. Resetting...", name);
				ResetInstance();
			}
			_instance = instance;
			_networkController = INetworkManager.GetInstance().Network;
			_rpc = new RpcManager(IsAuthority, Send);
			_rpc.AddTarget(this, MY_RPC_RECEIVER_ID);
			// StartMessageThread();
			NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Initializing NetworkIdentity {name} with id {id}", name, InstanceId);
			_isInitialized = true;
		}

		protected virtual void Update()
		{
			if (false == _isInitialized) return;
			
			Instance.Update();
		}

		#region Messaging
		
		
		public virtual bool HandleMessage(INetworkMessage incoming)
		{
			if (_isInitialized == false)
			{
				return false;
			}

			switch (incoming)
			{
				case RpcCall rpc:
					RPC.Process(rpc);
					break;
				case SyncCall sync:
					RPC.Process(sync);
					break;
				default:
					NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Message {msg} was not recognized", incoming);
					break;
			}
			
			return true;
		}

		protected void Send(INetworkMessage message, ushort channel)
		{
			if (Network == null)
			{
				// Network is not active
				return;
			}
			if (IsInitialized == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} was not initialized", name);
				return;
			}
			message.SourceInstance = InstanceId;
			message.Time = NetworkTime.LocalTime;
			Network.Broadcast(InstanceId, message, channel);
		}

		#endregion

		#region RPC

		protected void AddRpcTarget(object target, ushort id)
		{
			_rpc.AddTarget(target, id);
		}

		protected void RemoveRpcTarget(ushort id)
		{
			_rpc.RemoveTarget(id);
		}
		
		protected void Rpc(Action function) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID);
		protected void Rpc<T>(Action<T> function, T arg0) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0);
		protected void Rpc<T0, T1>(Action<T0, T1> function, T0 arg0, T1 arg1) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0, arg1);
		protected void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, T0 arg0, T1 arg1, T2 arg2) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0, arg1, arg2);

		#endregion

		#region Poolable

		protected virtual void ResetInstance()
		{
			_isInitialized = false;
			_instance = null;
			_rpc?.Clear();
			_rpc = null;
		}
		
		public virtual void OnSpawn()
		{
			// ResetInstance();
		}

		public virtual void OnDespawn()
		{
			ResetInstance();
		}

		public virtual void Despawn()
		{
			NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{name} is not available for {this}", nameof(Despawn), this);
		}

		public virtual void DespawnLocal()
		{
			NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{name} is not available for {this}", nameof(DespawnLocal), this);
		}

		#endregion
	}
}
