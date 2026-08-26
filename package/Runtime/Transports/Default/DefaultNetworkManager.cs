using System;
using System.Net;
using Soso.Net.Logging;
using Soso.Net.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Soso.Net.Transports.Default
{
    public class DefaultNetworkManager : INetworkManager
    {
        [SerializeField] public double LagSimulationSeconds;
        [SerializeField] private string Ip;
        [SerializeField] private int Port;
        [SerializeField] private string ServerUrl;
        
        [SerializeField] public bool ShouldSpoofPlayer;
        [SerializeField] public ulong PlayerId;
        
        public override bool IsOffline => _connectionManager == null;
        
        private DefaultConnectionManager _connectionManager;
        private DefaultSocketManager _socketManager;

        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying) return;

            if (ShouldSpoofPlayer && PlayerId == 0)
            {
                PlayerId = (ulong)Random.Range(0, 1000);
            }
        }

        protected override Awaitable ShutdownAsync()
        {
            Disconnect();
            
            return base.ShutdownAsync();
        }

        protected override async Awaitable<bool> CreateSocketServerInternal(ulong hostId, int virtualPort = 0)
        {
            _socketManager = await CreateSocketServerAsync(Ip, Port);
            if (_socketManager == null)
            {
                return false;
            }
            // _socketManager.OnUserConnected += OnUserConnected;
            _socketManager.OnUserDisconnected += UserDisconnected;

            return true;
        }

        protected override async Awaitable<bool> JoinSocketServerInternal(ulong host, int virtualPort = 0)
        {
            _connectionManager = await JoinSocketServerAsync(PlayerId, Ip, Port);
            if (_connectionManager != null)
            {
                _connectionManager.OnDisconnect += OnDisconnected;
            }
            return _connectionManager != null;
        }

        protected override void DoUpdate()
        {
            try
            {
                SosoNetwork.Process();
                
                _socketManager?.Process();
                
                _connectionManager?.Process();
            }
            catch (Exception e)
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Process failed for network with error: {msg}\n{st}", e.Message, e.StackTrace);
            }
        }

        protected override void DisconnectInternal()
        {
            if (_connectionManager != null)
            {
                _connectionManager.Shutdown();
                _connectionManager = null;
            }

            if (_socketManager != null)
            {
                _socketManager.Shutdown();
                _socketManager = null;
            }
        }

        public override void Send<T>(T message, int channel, SOSO_SEND_TYPE sendType)
        {
            if (_connectionManager == null)
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, $"Tried to send message type: {typeof(T).Name} but connection manager was null");
                return;
            }
            NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, $"{nameof(Send)} - Msg: {message} Chl: {channel}");
            _connectionManager.MyConnection.Send(message, channel, sendType);
        }

        public override void Broadcast<T>(T message, int channel, SOSO_SEND_TYPE sendType)
        {
            if (_socketManager == null)
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, $"Tried to send message type: {typeof(T).Name} but socket manager was null");
                return;
            }
            
            NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, $"{nameof(Broadcast)} - Msg: {message} Chl: {channel}");
            _socketManager.Broadcast(message, channel, sendType);
        }

        public override bool IsHost()
        {
            return _socketManager != null || IsOffline;
        }

        public override ulong GetClientId()
        {
            return PlayerId; //_connectionManager.Connection.GetId();
        }

        #region Network
        
        public static async Awaitable<DefaultSocketManager> CreateSocketServerAsync(string ip, int port)
        {
            var socketManager = SosoNetwork.CreateListener<DefaultSocketManager>(new IPEndPoint(IPAddress.Parse(ip), port));

            return socketManager;
        }

        public static async Awaitable<DefaultConnectionManager> JoinSocketServerAsync(ulong id, string ip, int port)
        {
            DefaultConnectionManager connectionManager;
            try
            {
                connectionManager = await SosoNetwork.ConnectAsync<DefaultConnectionManager>(id, new IPEndPoint(IPAddress.Parse(ip), port));
            }
            catch (Exception e)
            {
                NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Failed to connect to {ip}:{port} with error: {e.Message}", ip, port, e.Message);
                connectionManager = null;
            }
            return connectionManager;
        }

        #endregion
    }
}