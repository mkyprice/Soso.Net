using Soso.Net.Behaviors;
using System;
using System.Threading;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.PredictedBodies;
using Soso.Net.Core;
using Soso.Net.Logging;
using Soso.Net.Messaging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Net.Serializers;
using Soso.Net.Utils;
using Soso.Serialization;
using Soso.Serialization.Binary;
using UnityEngine;

namespace Soso.Net
{
	public abstract class INetworkManager : SosoNetSingleton<INetworkManager>
	{
		[SerializeField] public INetworkSpawner Spawner;
		[SerializeField] public NetworkController Network;
		[SerializeField] public NetworkInstanceManager Instances;
		[SerializeField] private long _localTimeDebug;
		[SerializeField, Range(1, 100)] private long _pingIntervalMs = 25;
		
		public MessageProcessorManager ClientProcessor = new MessageProcessorManager();
		public MessageProcessorManager ServerProcessor = new MessageProcessorManager();
		
		public Action OnDisconnected;

		public bool IsOffline { get; private set; } = true;
		public bool IsServer { get; private set; } = false;
		public bool IsClient { get; private set; } = false;
		
		public SessionManager Session { get; private set; }
		private bool _isConnected = false;
		public bool IsConnected => _isConnected;
		public static SessionInfo SessionInfo
		{
			get
			{
				if (TryGetInstance(out INetworkManager inst) == false)
				{
					NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No network manager found");
					return default;
				}
				var session = inst.Session.Session;
				if (session == null)
				{
					NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "No session info. Are you offline?");
					return default;
				}
				return session.Value;
			}
		}

		public static ushort SessionId
		{
			get
			{
				if (SessionInfo == default)
				{
					return 0;
				}
				return SessionInfo.SessionId;
			}
		}

		public static NetworkInstanceManager GetInstanceManager()
		{
			if (TryGetInstance(out INetworkManager inst) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No network manager found");
				return null;
			}
			return inst.Instances;
		}

		public static NetworkController GetController()
		{
			if (TryGetInstance(out INetworkManager inst) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No network manager found");
				return null;
			}
			return inst.Network;
		}

		public static INetworkSpawner GetSpawner()
		{
			if (TryGetInstance(out INetworkManager inst) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No network manager found");
				return null;
			}
			return inst.Spawner;
		}
		public static TSpawner GetSpawner<TSpawner>()
			where TSpawner : INetworkSpawner
		{
			return GetSpawner() as TSpawner;
		}

		protected RequestHandler _requestHandler;

		protected override Awaitable InitializeAsync()
		{
			// Register Commands
			SosoSerializer.Config
				.AddSerializer(new QuaternionSerializer())
				.AddSerializer(new NetworkInstanceIdSerializer())
				.AddSerializer(new Vector2Serializer())
				.AddSerializer(new NetworkPacket.Serializer())
				.AddSerializer(new NetworkMessage.Serializer())
				.AddSerializer(new SpawnCommand.Serializer())
				.AddSerializer(new DespawnCommand.Serializer())
				.AddSerializer(new SyncTransform.Serializer())
				.AddSerializer(new RpcCall.Serializer())
				.AddSerializer(new SyncCall.Serializer())
				.AddSerializer(new SyncRigidBody2D.Serializer())
				.AddSerializer(new SessionInfo.Serializer())
				.AddSerializer(new SessionNegotiation.Serializer())
				.AddSerializer(new RequestPacket.Serializer())
				.AddSerializer(new ResponsePacket.Serializer())
				.AddSerializer(new RigidBody2DState.Serializer());
			
			// Register mappings
			// SosoSerializer.Config
			// 	.AddMapping<INetworkMessage, NetworkMessage>()
			// 	.AddMapping<INetworkMessage, SpawnCommand>()
			// 	.AddMapping<INetworkMessage, DespawnCommand>();

			IsOffline = true;
			IsServer = false;
			IsClient = false;
			
			Instances.Initialize();
			
			// Initialize Network Time
			NetworkTime.Initialize();
			NetworkTime.PingIntervalMs = _pingIntervalMs;
			
			// Initialize Session
			Session = new SessionManager(this);
			_requestHandler = new RequestHandler(this);
			
			// Initialize spawner
			Spawner.InitializeSpawner();
			
			// Offline
			StartOfflineMode();
			
			return base.InitializeAsync();
		}

		protected override Awaitable ShutdownAsync()
		{
			if (IsServer || IsClient)
			{
				Disconnect();
			}
			
			return base.ShutdownAsync();
		}

		public void AddRequestHandler(object requestHandler)
		{
			RequestCache.Add(requestHandler);
		}
		
		public static async Awaitable<T> RequestAsync<T>(string path, params object[] args)
		{
			if (TryGetInstance(out INetworkManager inst) == false)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No network manager found");
				return default;
			}
			CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			return await inst._requestHandler.RequestAsync<T>(tokenSource.Token, path, args);
		}

		public void StartOfflineMode()
		{
			IsOffline = true;
			IsServer = true;
			IsClient = true;
			_isConnected = false;
			Instances.Initialize();
			Session.CreateOfflineSession(GetClientId());
			Network.Initialize();
			Spawner.InitializeSpawner();
		}
		
		public async Awaitable<bool> CreateSocketServer(ulong hostId, int virtualPort = 0)
		{
			if (IsOffline)
			{
				Cleanup();
			}
			else if (IsConnected || IsClient || IsServer)
			{
				Disconnect();
			}
			
			if (await CreateSocketServerInternal(hostId, virtualPort))
			{
				IsOffline = false;
				IsServer = true;
				return true;
			}
			return false;
		}
		protected abstract Awaitable<bool> CreateSocketServerInternal(ulong hostId, int virtualPort = 0);

		public async Awaitable<bool> JoinSocketServer(ulong host, int virtualPort = 0)
		{
			if (IsOffline)
			{
				Cleanup();
			}
			else if (IsClient)
			{
				Disconnect();
			}
			if (await JoinSocketServerInternal(host, virtualPort))
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Joined socket server. Negotiating ID...");
				if (await Session.NegotiateId(GetClientId()) == false)
				{
					NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to negotiate ID");
					return false;
				}
				IsOffline = false;
				IsClient = true;
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Successfully negotiated ID {session}", SessionInfo);
				Instances.Initialize();
				Network.Initialize();
				Spawner.InitializeSpawner();
				_isConnected = true;
				return true;
			}
			Cleanup();
			return false;
		}
		public void Disconnect()
		{
			DisconnectInternal();
			
			Cleanup();
		}

		protected virtual void Cleanup()
		{
			_isConnected = false;
			IsOffline = true;
			IsServer = false;
			IsClient = false;
			Spawner.Reset();
			Network.Shutdown();
			Instances.Reset();
			NetworkTime.Clear();
		}

		protected abstract Awaitable<bool> JoinSocketServerInternal(ulong host, int virtualPort = 0);

		private MessageSerializer _serializer = new MessageSerializer(8 * 1024 * 1024);
		public void Send<T>(T message, int channel, SOSO_SEND_TYPE sendType)
		{
			lock (_serializer)
			{
				int length = _serializer.Write(message);
				Send(_serializer.SendBuffer, 0, length, sendType, channel);
			}
		}

		public void Broadcast<T>(T message, int channel, SOSO_SEND_TYPE sendType)
		{
			lock (_serializer)
			{
				int length = _serializer.Write(message);

				foreach (var session in Session.GetUsers())
				{
					SendTo(_serializer.SendBuffer, 0, length, sendType, session.ConnectionId, channel);
				}
			}
		}
		
		protected abstract void Send(byte[] data, int offset, int length, SOSO_SEND_TYPE sendType, int channel);
		protected abstract void SendTo(byte[] data, int offset, int length, SOSO_SEND_TYPE sendType, ulong connectionId, int channel);
		protected abstract void DisconnectInternal();
		public abstract ulong GetClientId();

		protected virtual void DoUpdate()
		{ }

		/// <summary>
		/// Must be called by inheritors
		/// </summary>
		/// <param name="connection"></param>
		protected void UserDisconnected(IUserConnection connection)
		{
			if (IsServer)
			{
				Session.RemoveUser(connection.Id);
			}
		}

		private void Update()
		{
			NetworkTime.Update();
			_localTimeDebug = NetworkTime.Time;
			
			Network?.Update();
			
			DoUpdate();
		}
	}
}
