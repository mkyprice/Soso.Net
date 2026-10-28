using System;
using System.Collections.Generic;
using Soso.Net.Behaviors;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Serialization;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net
{
    public static class NetworkTime
    {
        public static long Time => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _clientServerDeltaMs;
        private static long _lastPingTime;
        public static long PingIntervalMs = 5;
        
        private static readonly Dictionary<ushort, RemoteInfo> _connectionInfo = new Dictionary<ushort, RemoteInfo>();
        private static readonly int PING_CHANNEL = 0;
        public static long TimeAdjustmentMs { get; set; }
        private static long _clientServerDeltaMs;
        private static long[] _clientServerDeltaWindow = new long[10];
        private static int _clientServerDeltaWindowIndex = 0;

        public static void Initialize()
        {
            SosoSerializer.Config
                .AddSerializer(new PingPacket.Serializer());

            _clientServerDeltaMs = 0;
            
            var network = INetworkManager.GetInstance();
            network.ClientProcessor.Subscribe<PingPacket>(PING_CHANNEL, OnClientPingMessageReceived);
            network.ServerProcessor.Subscribe<PingPacket>(PING_CHANNEL, OnServerPingMessageReceived);
        }

        public static void Clear()
        {
            _connectionInfo.Clear();
        }

        private static void Ping(ushort sessionId)
        {
            var network = INetworkManager.GetInstance();
            if (network.IsConnected == false)
            {
                // We are not connected
                return;
            }
            _lastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PingPacket ping = new PingPacket();
            ping.SourceId = sessionId;
            ping.RealtimeSendMs = _lastPingTime;
            NetworkLogger.Debug(CHANNEL.Default, "Sending ping with id {id} and time {time}", ping.SourceId, _lastPingTime);
            network.Send(ping, 0, SOSO_SEND_TYPE.Reliable);
        }
        
        public static bool IsPingReady()
        {
            long localTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return localTime - _lastPingTime >= PingIntervalMs;
        }
        
        public static long ToLocalTime(ushort netId, long remoteTime)
        {
            if (_connectionInfo.TryGetValue(netId, out var info))
            {
                return info.ToLocalTime(remoteTime);
            }
            return remoteTime;
        }
        
        private static void OnServerPingMessageReceived(PingPacket ping, long arg2, long arg3, IUserConnection connection)
        {
            // Forward message to all
            ping.ServerTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            INetworkManager.GetInstance().Broadcast(ping, PING_CHANNEL, SOSO_SEND_TYPE.Reliable);
        }
        
        private static void OnClientPingMessageReceived(PingPacket packet, long arg2, long arg3, IUserConnection connection)
        {
            ushort source = packet.SourceId;
            
            long realtimeReceiveMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Calculate round trip time
            long rtt = realtimeReceiveMs - packet.RealtimeSendMs;
            long serverTimeAtArrival = packet.ServerTimeMs + (rtt / 2);
            var clientServerDeltaMs = serverTimeAtArrival - realtimeReceiveMs;
            
            // Calculate our own RTT
            if (source == INetworkManager.SessionId)
            {
                _clientServerDeltaWindow[_clientServerDeltaWindowIndex] = clientServerDeltaMs;
                _clientServerDeltaWindowIndex++;
                if (_clientServerDeltaWindowIndex >= _clientServerDeltaWindow.Length) _clientServerDeltaWindowIndex = 0;
                long avg = 0;
                int totalAvged = 0;
                foreach (var delta in _clientServerDeltaWindow)
                {
                    if (delta == 0)
                    {
                        break;
                    }
                    totalAvged++;
                    avg += delta;
                }
                if (totalAvged > 0)
                {
                    avg /= totalAvged;
                }
                _clientServerDeltaMs = avg;
                return;
            }
            
            
            // Figure out other users time to get to us
            if (_connectionInfo.TryGetValue(source, out var info) == false)
            {
                info = new RemoteInfo()
                {
                    SessionId = source,
                };
                _connectionInfo.Add(source, info);
            }
			
            // Set remote info
            info.AddPingTime(clientServerDeltaMs);
            info.RemoteTime = packet.RealtimeSendMs;
            info.LastPingTime = realtimeReceiveMs;
            
            // Give our info
            var instances = INetworkManager.GetInstanceManager();
            if (instances != null)
            {
                foreach (var instance in instances.GetOwnedInstances(source))
                {
                    instance.SetRemoteInfo(info);
                }
            }
        }

        public static RemoteInfo GetRemoteInfo(ushort sessionId)
        {
            if (_connectionInfo.TryGetValue(sessionId, out var info))
            {
                return info;
            }
            RemoteInfo remoteInfo = new RemoteInfo()
            {
                SessionId = sessionId,
            };
            if (sessionId == INetworkManager.SessionId)
            {
                // This is us, we can just add it with no remote time
                remoteInfo.PingMs = 0;
                remoteInfo.RemoteTime = Time;
                _connectionInfo.Add(sessionId, remoteInfo);
            }
            else
            {
                NetworkLogger.Warn(CHANNEL.Default, "No remote info found for network id {id}", sessionId);
            }
            return remoteInfo;
        }

        public static void Update()
        {
            var network = INetworkManager.GetInstance();
            if (network.IsOffline || network.IsConnected == false || network.Session == null) return;
            if (IsPingReady())
            {
                Ping(INetworkManager.SessionId);
                if (network.IsServer)
                {
                    Ping(SessionInfo.SERVER_SESSION_ID);
                }
            }
        }
    }
}