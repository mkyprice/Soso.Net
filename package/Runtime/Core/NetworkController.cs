using Soso.Net.Components;
using System.Collections.Generic;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Net.Objects;
using Soso.Utils.Concurrency;
using System;
using System.Collections.Concurrent;
using Soso.Net.Core;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Behaviors
{
	[Serializable]
	public sealed class NetworkController
	{
		[SerializeField, Range(0, 10)] public float SendInterval = 0.05f;
		[SerializeField] public double TimelineOffset = 0.1f;

		[Header("Debug - DO NOT MODIFY")] 
		[SerializeField] private ushort _sessionId;
		
		public INetworkManager Network => INetworkManager.GetInstance();

		public bool IsHost => Network.IsHost();
		public ushort SessionId => INetworkManager.SessionId;
		
		/// <summary>
		/// Attempt to adjust to remote time given send interval and offset
		/// </summary>
		public double TimeAdjustment => SendInterval + TimelineOffset;
		
		private SosoConcurrentQueue<INetworkMessage> _sendQueue = new SosoConcurrentQueue<INetworkMessage>();
		private double _lastSendTime;
		private double _lastPingTime;
		private bool _isInitialized = false;

		public void Initialize()
		{
			if (_isInitialized)
			{
				return;
			}
			if (Network.IsOffline)
			{
				_isInitialized = true;
				NetworkLogger.Warn(CHANNEL.Default, "You are offline. No messages will be processed");
				return;
			}
			
			_sessionId = INetworkManager.SessionId;
			
			NetworkTime.TimeAdjustment = TimeAdjustment;

			Network.ClientProcessor.Subscribe<NetworkPacket>(0, OnClientNetworkMessageReceived);
			Network.ServerProcessor.Subscribe<NetworkPacket>(0, OnServerNetworkMessageReceived);
			
			_isInitialized = true;
		}

		public void Shutdown()
		{
			_isInitialized = false;
			
			Clear();
			
			Network.ClientProcessor.Unsubscribe<NetworkPacket>(0, OnClientNetworkMessageReceived);
			Network.ServerProcessor.Unsubscribe<NetworkPacket>(0, OnServerNetworkMessageReceived);
		}

		public void Broadcast(NetworkInstanceId instanceId, INetworkMessage data, ushort channel)
		{
			_sendQueue.EnqueueBack(data);
		}

		internal void Update()
		{
			if (_isInitialized == false)
			{
				return;
			}
			double localTime = NetworkTime.LocalTime;
			if (IsSendReady())
			{
				if (_sendQueue.Count > 0)
				{
					_lastSendTime = localTime;
					
					NetworkPacket packet = new NetworkPacket();
					packet.SourceId = SessionId;
					packet.SendTime = localTime;
					packet.Messages = _sendQueue.ToArray();
					_sendQueue.Clear();

					NetworkLogger.Debug(CHANNEL.Default, "Sending {count} messages in packet", packet.Messages.Length);
					// Send to server
					Network.Send(packet, 0, SOSO_SEND_TYPE.Reliable);
				}
			}
		}

		public void Clear()
		{
			_sendQueue.Clear();
		}

		private bool IsSendReady()
		{
			if (Network.IsConnected == false)
			{
				return false;
			}
			double localTime = NetworkTime.LocalTime;
			return localTime - _lastSendTime >= SendInterval;
		}

		private void ProcessIncomingMessage(RemoteInfo remoteInfo, INetworkMessage message)
		{
			if (message.Destination == DESTINATION.Client && remoteInfo.SessionId == SessionId)
			{
				// I sent the message
				return;
			}
			
			// Adjust time to be in local time
			double adjustedLocalTime;
			if (remoteInfo.SessionId == INetworkManager.SessionId)
			{
				adjustedLocalTime = message.Time;
			}
			else
			{
				adjustedLocalTime = NetworkTime.ToLocalTime(remoteInfo, message.Time);
			}
			message.Time = adjustedLocalTime;

			NetworkLogger.Debug(CHANNEL.Default, "Processing message {msg} from {re}", message, remoteInfo);
			
			// Enqueue our message
			NetworkInstanceId target = message.SourceInstance;
			var instance = INetworkManager.GetInstanceManager().GetOrCreateInstance(target);
			instance.AppendMessage(message);
		}

		#region Message Callbacks

		private void OnServerNetworkMessageReceived(NetworkPacket packet, long a, long b, IUserConnection connection)
		{
			ServerProcessPacket(packet);
		}

		private void OnClientNetworkMessageReceived(NetworkPacket packet, long a, long b, IUserConnection connection)
		{
			var remoteInfo = NetworkTime.GetRemoteInfo(packet.SourceId);
			foreach (INetworkMessage message in packet.Messages)
			{
				ProcessIncomingMessage(remoteInfo, message);
			}
		}

		#endregion

		private void ServerProcessPacket(NetworkPacket packet)
		{
			List<INetworkMessage> forwardMessages = new List<INetworkMessage>();
			var remoteInfo = NetworkTime.GetRemoteInfo(packet.SourceId);

			foreach (INetworkMessage message in packet.Messages)
			{
				if (message.Destination == DESTINATION.Client)
				{
					forwardMessages.Add(message);
				}
				else if (message.Destination == DESTINATION.Server)
				{
					ProcessIncomingMessage(remoteInfo, message);
				}
			}

			if (forwardMessages.Count > 0)
			{
				NetworkPacket forwardPacket = new NetworkPacket();
				forwardPacket.SourceId = packet.SourceId;
				forwardPacket.Messages = forwardMessages.ToArray();
				forwardPacket.SendTime = packet.SendTime;
				
				Network.Broadcast(forwardPacket, 0, SOSO_SEND_TYPE.Reliable);
			}
		}
	}
}
