using System;
using System.Collections.Concurrent;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Net.Models.Packets;
using Soso.Net.Utils;
using Soso.Utils;
using UnityEngine;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net
{
    public class SessionManager
    {
        public SessionInfo? Session { get; private set; }
        public Action<SessionInfo> OnUserConnected;
        public Action<SessionInfo> OnUserDisconnected;
        
        private ushort _nextSessionId = SessionInfo.SERVER_SESSION_ID;
        private object _sessionIdLock = new object();
        private ConcurrentDictionary<ulong, SessionInfo> _sessionMap = new ConcurrentDictionary<ulong, SessionInfo>();

        private INetworkManager _network;

        public SessionManager(INetworkManager network)
        {
            _network = network;
            _network.ServerProcessor.Subscribe<SessionNegotiation>(0, OnServerIdNegotiation);
            _network.ClientProcessor.Subscribe<SessionNegotiation>(0, OnClientIdNegotiation);
            _network.ClientProcessor.Subscribe<SessionInfo>(0, OnUserJoined);
            _network.ClientProcessor.Subscribe<SessionInfo>(1, OnUserLeft);
        }

        public async Awaitable<bool> NegotiateId(ulong myNetworkId)
        {
            Clear();
            
            _network.Send(new SessionNegotiation()
            {
                Session = new SessionInfo()
                {
                    ConnectionId = myNetworkId
                }
            }, 0, SOSO_SEND_TYPE.Reliable);

            int waitAttempts = 1000;
            while (Session == null)
            {
                if (--waitAttempts <= 0)
                {
                    NetworkLogger.Error(CHANNEL.Default, "Failed to negotiate session");
                    return false;
                }
                await Awaitable.WaitForSecondsAsync(0.5f);
                
                NetworkLogger.Info(CHANNEL.Default, "Waiting for session...");
            }

            return true;
        }

        public SessionInfo[] GetUsers()
        {
            return _sessionMap.Values.ToArray();
        }
        
        public void Clear()
        {
            Session = null;
            lock (_sessionIdLock)
            {
                _sessionMap.Clear();
                _nextSessionId = SessionInfo.SERVER_SESSION_ID; // IMPORTANT! Server ID IS 1
            }
        }
        
        public bool TryGetSessionInfo(ushort sessionId, out SessionInfo info)
        {
            foreach (var session in _sessionMap.Values)
            {
                if (session.SessionId == sessionId)
                {
                    info = session;
                    return true;
                }
            }
            info = default;
            return false;
        }
        
        public bool TryGetSessionInfo(ulong connectionId, out SessionInfo info)
        {
            return _sessionMap.TryGetValue(connectionId, out info);
        }

        public bool Contains(ulong clientId)
        {
            return _sessionMap.ContainsKey(clientId);
        }

        public SessionInfo CreateOfflineSession(ulong networkId)
        {
            var session = BuildClient(networkId);
            if (Session == null)
            {
                Session = session;
            }
            AddUser(session, networkId);
            return session;
        }

        public void RemoveUser(ulong connectionId)
        {
            if (_sessionMap.TryRemove(connectionId, out SessionInfo session))
            {
                INetworkManager.GetInstance().Broadcast(session, 1, SOSO_SEND_TYPE.Reliable);
            }
        }

        #region Callbacks

        private void OnClientIdNegotiation(SessionNegotiation negotiation, long arg2, long arg3, IUserConnection connection)
        {
            Session = negotiation.Session;

            foreach (var existingUser in negotiation.ExistingUsers)
            {
                AddUser(existingUser, existingUser.ConnectionId);
            }
        }

        private MessageSerializer _serializer = new MessageSerializer();
        private void OnServerIdNegotiation(SessionNegotiation negotiation, long arg2, long arg3, IUserConnection connection)
        {
            var client = BuildClient(connection.Id);

            var returnNegotiation = new SessionNegotiation()
            {
                Session = client,
                ExistingUsers = _sessionMap.Values.ToArray(),
            };
            int length = _serializer.Write(returnNegotiation);
            connection.Send(_serializer.SendBuffer, 0, length, SOSO_SEND_TYPE.Reliable, 0);
            
            INetworkManager.GetInstance().Broadcast(client, 0, SOSO_SEND_TYPE.Reliable);
        }

        private void OnUserJoined(SessionInfo user, long arg2, long arg3, IUserConnection connection)
        {
            AddUser(user, connection.Id);
        }

        private void OnUserLeft(SessionInfo user, long arg2, long arg3, IUserConnection connection)
        {
            RemoveUser(user, connection.Id);
        }

        #endregion

        #region Private Methods
        
        private SessionInfo BuildClient(ulong networkId)
        {
            ushort nextId;
            lock (_sessionIdLock)
            {
                _nextSessionId++;
                nextId = _nextSessionId;
            }

            var client = new SessionInfo()
            {
                SessionId = nextId,
                ConnectionId = networkId
            };

            _sessionMap.TryAdd(networkId, client);
            
            return client;
        }

        private void AddUser(SessionInfo user, ulong connectionId)
        {
            if (_sessionMap.TryAdd(connectionId, user))
            {
                OnUserConnected?.Invoke(user);
            }
        }

        private void RemoveUser(SessionInfo user, ulong connectionId)
        {
            if (_sessionMap.TryRemove(connectionId, out SessionInfo session))
            {
                OnUserDisconnected?.Invoke(session);
            }
        }

        #endregion
    }
}