using System;
using System.Collections.Generic;
using Soso.Net.Logging;
using Soso.Net.Models;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Transports.Default
{
    public class DefaultSocketManager : SosoSocketManager
    {
        public Action<IUserConnection> OnUserConnected;
        public Action<IUserConnection> OnUserDisconnected;
        
        private byte[] _sendBuffer = new byte[1 * 1024 * 1024];
        
        public IEnumerable<DefaultUserConnection> Connections => _connections.Values;
        private readonly Dictionary<SosoSocket, DefaultUserConnection> _connections = new Dictionary<SosoSocket, DefaultUserConnection>();

        public DefaultSocketManager(ISocketListener listener) : base(listener)
        {
        }

        public void Process()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Process();
            }
        }

        public void Broadcast<T>(T message, int channel, SOSO_SEND_TYPE sendType)
        {
            ByteWriter writer = new ByteWriter(_sendBuffer);

            writer.Write(channel);
            
            SosoSerializer.Serialize(ref writer, message, SerializationFlags.EmbedType);

            int length = writer.Position;

            Broadcast(new Span<byte>(_sendBuffer).Slice(0, length), 0);
        }
        
        public override bool AcceptConnection(SosoSocket connection)
        {
            return true;
        }

        public override void ConnectionChanged(SosoSocket connection, CONNECTION_STATUS status)
        {
            switch (status)
            {
                case CONNECTION_STATUS.Connected:
                    OnConnected(connection);
                    break;
                case CONNECTION_STATUS.Disconnected:
                    OnDisconnected(connection);
                    break;
            }
        }
        
        private void OnDisconnected(SosoSocket obj)
        {
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default,  $"Disconnected from {obj}");
            if (_connections.Remove(obj, out var connection))
            {
                OnUserDisconnected?.Invoke(connection);
                NetworkLogger.Info(NetworkLogger.CHANNEL.Default, $"Removed connection {obj}");
            }
        }

        private void OnConnected(SosoSocket obj)
        {
            var connection = new DefaultUserConnection(obj, INetworkManager.GetInstance().ServerProcessor);
            _connections[obj] = connection;
            // connection.Disconnected(OnDisconnected);
            OnUserConnected?.Invoke(connection);
            NetworkLogger.Info(NetworkLogger.CHANNEL.Default, $"Added connection {obj}");
        }

        public override void OnMessage(SosoSocket connection, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
        {
            NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, $"{nameof(DefaultSocketManager)}{connection.GetConnectionId()} - Received message of size {data.Length}, num: {messageNumber}, time: {timestamp}, channel: {channel}");
            _connections[connection].HandleMessage(data, timestamp, messageNumber);
        }
    }
}