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
        private static double _lastPingTime;

        public static float PingInterval = 0.5f;
        /// <summary>
        /// Seconds since game start
        /// </summary>
        public static double LocalTime => Time.unscaledTimeAsDouble;
        
        private static Dictionary<ushort, RemoteInfo> ConnectionInfo = new Dictionary<ushort, RemoteInfo>();

        private static readonly int PING_CHANNEL = 0;
        public static double TimeAdjustment { get; set; }

        public static void Initialize()
        {
            SosoSerializer.Config
                .AddSerializer(new PingPacket.Serializer());

            var network = INetworkManager.GetInstance();
            network.ClientProcessor.Subscribe<PingPacket>(PING_CHANNEL, OnClientPingMessageReceived);
            network.ServerProcessor.Subscribe<PingPacket>(PING_CHANNEL, OnServerPingMessageReceived);
        }

        public static void Clear()
        {
            ConnectionInfo.Clear();
        }

        private static void Ping(ushort sessionId)
        {
            var network = INetworkManager.GetInstance();
            if (network.IsConnected == false)
            {
                // We are not connected
                return;
            }
            _lastPingTime = LocalTime;
            PingPacket ping = new PingPacket();
            ping.SourceId = sessionId;
            ping.LocalTimeSec = _lastPingTime;
            ping.RealtimeSendTicks = DateTimeOffset.UtcNow.Ticks;
            NetworkLogger.Debug(CHANNEL.Default, "Sending ping with id {id} and time {time}", ping.SourceId, ping.LocalTimeSec);
            network.Send(ping, 0, SOSO_SEND_TYPE.Reliable);
        }
        
        public static bool IsPingReady()
        {
            double localTime = LocalTime;
            return localTime - _lastPingTime >= PingInterval;
        }
        
        private static void OnServerPingMessageReceived(PingPacket ping, long arg2, long arg3, IUserConnection connection)
        {
            // Forward message to all
            INetworkManager.GetInstance().Broadcast(ping, PING_CHANNEL, SOSO_SEND_TYPE.Reliable);
        }
        
        public static double ServerToLocalTime(double remoteTime)
        {
            // Server SessionId will always be 1
            if (ConnectionInfo.TryGetValue(SessionInfo.SERVER_SESSION_ID, out var info))
            {
                return ToLocalTime(info, remoteTime);
            }
            return remoteTime;
        }
        
        public static double ToLocalTime(ushort netId, double remoteTime)
        {
            if (ConnectionInfo.TryGetValue(netId, out var info))
            {
                return ToLocalTime(info, remoteTime);
            }
            return remoteTime;
        }
        
        public static double ToLocalTime(RemoteInfo info, double remoteTime)
        {
            return remoteTime 
                   + info.TimeDifference 
                   + info.Ping * 0.001 // Convert to seconds
                   + TimeAdjustment;
        }
        
        private static void OnClientPingMessageReceived(PingPacket packet, long arg2, long arg3, IUserConnection connection)
        {
            ushort source = packet.SourceId;
            if (source == INetworkManager.SessionId)
            {
                return;
            }

            double localTime = LocalTime;
            long realtimeReceived = DateTimeOffset.UtcNow.Ticks;
            long pingTicks = realtimeReceived - packet.RealtimeSendTicks;
            double pingSeconds = TimeSpan.FromTicks(pingTicks).TotalSeconds;
            double ping = pingSeconds * 1000; // MS
            
            if (ConnectionInfo.TryGetValue(source, out var info) == false)
            {
                info = new RemoteInfo()
                {
                    SessionId = source,
                    RemoteTime = packet.LocalTimeSec,
                    TimeDifference = localTime - packet.LocalTimeSec - pingSeconds,
                };
                ConnectionInfo.Add(source, info);
            }
			
            // Sync remote time to our time
            double remoteTime = packet.LocalTimeSec
                + info.TimeDifference 
                - pingSeconds; // Pretty sure we should sub ping
            
            // Set remote info
            info.AddPingTime(ping);
            info.RemoteTime = remoteTime;
            info.LastPingTime = localTime;
            
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
            if (ConnectionInfo.TryGetValue(sessionId, out var info))
            {
                return info;
            }
            RemoteInfo remoteInfo = new RemoteInfo()
            {
                SessionId = sessionId,
                TimeDifference = 0,
            };
            if (sessionId == INetworkManager.SessionId)
            {
                // This is us, we can just add it with no remote time
                remoteInfo.TimeDifference = 0;
                remoteInfo.RemoteTime = LocalTime;
                ConnectionInfo.Add(sessionId, remoteInfo);
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
            if (network.IsOffline || network.Session == null) return;
            if (IsPingReady())
            {
                Ping(INetworkManager.SessionId);
                if (network.IsHost())
                {
                    Ping(SessionInfo.SERVER_SESSION_ID);
                }
            }
        }
    }
}