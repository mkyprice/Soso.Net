using Soso.Net.Behaviors;
using System;
using System.Threading;
using Soso.Net.Behaviors.Rpc;
using Soso.Net.Components;
using Soso.Net.Components.NetworkedBodies.Packets;
using Soso.Net.Components.PredictedBodies;
using Soso.Net.Logging;
using Soso.Net.Messaging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Net.Serializers;
using Soso.Serialization;
using UnityEngine;

namespace Soso.Net
{
	public abstract class INetworkManager : SosoNetSingleton<INetworkManager>
	{
		[SerializeField] public INetworkSpawner Spawner;
		[SerializeField] public NetworkController Network;
		[SerializeField] private double _localTimeDebug;
		[SerializeField, Range(0.01f, 10f)] private float _pingInterval = 0.5f;
		
		public MessageProcessorManager ClientProcessor = new MessageProcessorManager();
		public MessageProcessorManager ServerProcessor = new MessageProcessorManager();
		
		public Action OnDisconnected;
		
		public abstract bool IsOffline { get; }
		
		public SessionManager Session { get; private set; }
		public static bool IsConnected
		{
			get
			{
				if (TryGetInstance(out INetworkManager inst) == false)
				{
					return false;
				}
				var sessionManager = inst.Session;
				if (sessionManager == null)
				{
					return false;
				}
				return sessionManager.Session?.SessionId != 0;
			}
		}
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
					if (inst.IsOffline)
					{
						NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Starting offline mode");
						inst.StartOfflineMode();
						session = inst.Session.Session;
					}
					NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "No session found. Are you offline?");
					return session ?? default;
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
			
			// Initialize Network Time
			NetworkTime.Initialize();
			NetworkTime.PingInterval = _pingInterval;
			
			// Initialize Session
			Session = new SessionManager(this);
			_requestHandler = new RequestHandler(this);
			
			// Initialize spawner
			Spawner.InitializeSpawner();
			
			return base.InitializeAsync();
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
			Session.CreateOfflineSession(GetClientId());
			Spawner.InitializeSpawner();
		}
		
		public async Awaitable<bool> CreateSocketServer(ulong hostId, int virtualPort = 0)
		{
			if (await CreateSocketServerInternal(hostId, virtualPort))
			{
				Cleanup();
				return true;
			}
			return false;
		}
		protected abstract Awaitable<bool> CreateSocketServerInternal(ulong hostId, int virtualPort = 0);

		public async Awaitable<bool> JoinSocketServer(ulong host, int virtualPort = 0)
		{
			if (IsHost() == false)
			{
				Cleanup();
			}
			if (await JoinSocketServerInternal(host, virtualPort))
			{
				if (await Session.NegotiateId(GetClientId()) == false)
				{
					return false;
				}
				Network.Initialize();
				Spawner.InitializeSpawner();
				return true;
			}
			Session.Clear();
			return false;
		}
		public void Disconnect()
		{
			DisconnectInternal();
			
			Cleanup();
		}

		protected virtual void Cleanup()
		{
			Spawner.Reset();
			Network.Shutdown();
			NetworkTime.Clear();
		}

		protected abstract Awaitable<bool> JoinSocketServerInternal(ulong host, int virtualPort = 0);

		public abstract void Send<T>(T message, int channel, SOSO_SEND_TYPE sendType);
		public abstract void Broadcast<T>(T message, int channel, SOSO_SEND_TYPE sendType);
		protected abstract void DisconnectInternal();
		public abstract bool IsHost();
		public abstract ulong GetClientId();

		protected virtual void DoUpdate()
		{ }

		/// <summary>
		/// Must be called by inheritors
		/// </summary>
		/// <param name="connection"></param>
		protected void UserDisconnected(IUserConnection connection)
		{
			if (IsHost())
			{
				Session.RemoveUser(connection.Id);
			}
		}

		private void Update()
		{
			NetworkTime.Update();
			_localTimeDebug = NetworkTime.LocalTime;
			
			Network?.Update();
			
			DoUpdate();
		}
	}
}
