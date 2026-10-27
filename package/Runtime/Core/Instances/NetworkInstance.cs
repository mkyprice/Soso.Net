using System;
using Soso.Net.Behaviors;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Utils.Concurrency;
using UnityEngine;

namespace Soso.Net.Objects
{
	[Serializable]
	public class NetworkInstance
	{
		public static int MAX_MESSAGES_PER_UPDATE = 5;
		
		[SerializeField] private NetworkInstanceId _id;
		[SerializeField] private RemoteInfo _remoteInfo;
		[SerializeField] public int? Type;
		
		public INetworkIdentity Identity;
		public NetworkInstanceId Id => _id;
		public RemoteInfo RemoteInfo
		{
			get
			{
				if (_remoteInfo == null)
				{
					_remoteInfo = NetworkTime.GetRemoteInfo(Id.SessionId);
				}
				return _remoteInfo;
			}
		}
		private SosoConcurrentSortedQueue<double, INetworkMessage> _timedReceiveQueue = new SosoConcurrentSortedQueue<double, INetworkMessage>();
		private SosoConcurrentQueue<INetworkMessage> _readyQueue = new SosoConcurrentQueue<INetworkMessage>();
		
		public NetworkInstance(NetworkInstanceId id)
		{
			_id = id;
		}

		public void SetInstance(INetworkIdentity instance, int? type)
		{
			Identity = instance;
			Type = type;
			Identity.Initialize(this);
		}
		
		internal void AppendMessage(INetworkMessage incoming)
		{
			if (incoming.SyncTime)
			{
				NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{name} enqueuing message: {message} at time {t} (Local time: {lt})", Id, incoming, incoming.Time, NetworkTime.LocalTime);
				_timedReceiveQueue.Add(incoming.Time, incoming);
			}
			else
			{
				NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{name} Enqueuing ready message {message}", Id, incoming);
				_readyQueue.EnqueueBack(incoming);
			}
		}

		public void Update()
		{
			if (Identity == null)
			{
				return;
			}
			
			while (_timedReceiveQueue.TryDequeueFirst(out var message) && message != null)
			{
				float messageWait = (float)(message.Time - NetworkTime.LocalTime);
				if (messageWait > 0.001f)
				{
					_timedReceiveQueue.Add(message.Time, message);
					NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{id} waiting for {wait}s", Id, messageWait);
					break;
				}

				_readyQueue.EnqueueBack(message);
			}

			int processed = 0;
			while (processed < MAX_MESSAGES_PER_UPDATE && _readyQueue.TryDequeue(out var message))
			{
				NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, "{name} Processing message {message}", Id, message);
				processed++;
				if (Identity.HandleMessage(message) == false)
				{
					// Identity is not ready
					_readyQueue.EnqueueFront(message);
					break;
				}
			}
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override string ToString()
		{
			return $"[Type:{Type}, Id:{Id}, Name:{(Identity != null ? Identity.GetName() : "?")}]";
		}

		public void SetRemoteInfo(RemoteInfo value)
		{
			_remoteInfo = value;
		}
	}
}
