using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Utils.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Soso.Net.Behaviors
{
	public abstract class BaseNetworkInstance : MonoBehaviour, INetworkPoolable
	{
		[Header("DEBUG DO NOT MODIFY")]
		[SerializeField] public NetworkInstanceId InstanceId;
		public RemoteInfo RemoteInfo
		{
			get
			{
				if (_remoteInfo == null)
				{
					_remoteInfo = NetworkTime.GetRemoteInfo(OwnerId);
				}
				return _remoteInfo;
			}
		}
		private RemoteInfo _remoteInfo;

		public ushort OwnerId => InstanceId.SessionId;
		public ulong Sequence => InstanceId.SequenceNumber;
		
		public double RemoteTime => _remoteInfo?.RemoteTime ?? 0;
		public double Ping => _remoteInfo?.Ping ?? 0;

		public bool IsServer
		{
			get
			{
				return Network == null || Network.IsHost || Network.Network.IsOffline;
			}
		}
		
		public abstract bool IsOwner { get; }
		
		private static readonly ushort MY_RPC_RECEIVER_ID = ushort.MaxValue;
		
		public bool IsInitialized => _isInitialized;

		public NetworkController Network => _networkController;

		public RpcManager RPC => _rpc;
		
		private RpcManager _rpc;
		private NetworkController _networkController;
		private List<INetworkReceiver> _receivers;
		private SosoConcurrentSortedQueue<double, INetworkMessage> _timedReceiveQueue = new SosoConcurrentSortedQueue<double, INetworkMessage>();
		private bool _isInitialized = false;
		private CancellationTokenSource _cancellationTokenSource;
		
		internal void Initialize(NetworkInstanceId instanceId)
		{
			if (_isInitialized)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} was already initialized", name);
				return;
			}
			InstanceId = instanceId;
			_networkController = INetworkManager.GetInstance().Network;
			_rpc = new RpcManager(IsOwner, Send);
			_rpc.AddTarget(this, MY_RPC_RECEIVER_ID);
			// StartMessageThread();
			NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Initializing NetworkIdentity {name} with id {id}", name, InstanceId);

			_receivers = new List<INetworkReceiver>();
			var receivers = GetComponentsInChildren<INetworkReceiver>(true);
			for (ushort i = 0; i < receivers.Length; i++)
			{
				AddReceiver(receivers[i]);
			}
			_isInitialized = true;
		}

		protected virtual void Update()
		{
			if (false == _isInitialized) return;
			
			if (_timedReceiveQueue.TryDequeueFirst(out var message) && message != null)
			{
				float messageWait = (float)(message.Time - NetworkTime.LocalTime);
				if (messageWait > 0.001f)
				{
					// Place message back in the queue
					_timedReceiveQueue.Add(message.Time, message);
					NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "Waiting for {wait}s", messageWait);
					return;
				}
				HandleMessage(message);
			}
		}

		#region Receivers
		
		public void Deregister(INetworkReceiver receiver)
		{
			if (_receivers == null || _isInitialized == false) return;
			
			ushort index = (ushort)(_receivers.IndexOf(receiver));
			if (index > 0)
			{
				_rpc.RemoveTarget(index);
				_receivers.Remove(receiver);
			}
		}

		private bool AddReceiver(INetworkReceiver receiver)
		{
			if (_receivers.Contains(receiver))
			{
				return false;
			}
			ushort id = (ushort)(_receivers.Count);
			receiver.Initialize(id, this);
			_receivers.Add(receiver);
			_rpc.AddTarget(receiver, id);
			return true;
		}

		private void AddReceiverTree(GameObject root)
		{
			var receivers = root.GetComponentsInChildren<INetworkReceiver>(true);
			foreach (var receiver in receivers)
			{
				AddReceiver(receiver);
			}
		}

		public T AddComponent<T>() 
			where T : INetworkReceiver
		{
			Rpc(RpcAddComponent, typeof(T).AssemblyQualifiedName);
			return GetComponent<T>();
		}

		internal void SetRemoteInfo(RemoteInfo remote)
		{
			_remoteInfo = remote;
		}

		#endregion

		#region Messaging
		
		internal void AppendMessage(INetworkMessage incoming)
		{
			if (this == false)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "{name} is destroyed. Not processing message", name);
				return;
			}
			if (incoming.SyncTime)
			{
				NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{name} enqueuing message: {message} at time {t} (Local time: {lt})", gameObject.name, incoming, incoming.Time,
					NetworkTime.LocalTime);
				_timedReceiveQueue.Add(incoming.Time, incoming);
			}
			else
			{
				HandleMessage(incoming);
			}
		}
		
		protected virtual void HandleMessage(INetworkMessage incoming)
		{
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
		
		protected void Rpc(Action function) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID);
		protected void Rpc<T>(Action<T> function, T arg0) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0);
		protected void Rpc<T0, T1>(Action<T0, T1> function, T0 arg0, T1 arg1) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0, arg1);
		protected void Rpc<T0, T1, T2>(Action<T0, T1, T2> function, T0 arg0, T1 arg1, T2 arg2) => _rpc.Rpc(function, MY_RPC_RECEIVER_ID, arg0, arg1, arg2);

		#endregion

		#region Poolable

		protected void ResetInstance()
		{
			_isInitialized = false;
			InstanceId = default;
			_remoteInfo = null;
			_timedReceiveQueue.Clear();
			_receivers?.Clear();
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

		#region RPC Functions
		
		[SosoRpc(RPC_CALL_TYPE.Client, true, true)]
		private void RpcAddComponent(string typeName)
		{
			Type type = Type.GetType(typeName);
			if (type == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Cannot find type {typeName}", typeName);
				return;
			}
			var receiver = gameObject.AddComponent(type) as INetworkReceiver;
			AddReceiver(receiver);
		}

		#endregion
	}
}
