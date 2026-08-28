using System;
using Soso.Net.Logging;

namespace Soso.Net.Transports.Default
{
    public class DefaultConnectionManager : SosoConnectionManager
    {
        public Action OnConnect { get; set; }
        public Action OnDisconnect { get; set; }
        public DefaultUserConnection MyConnection;

        public DefaultConnectionManager(ulong id, ISocketConnection connection) : base(id, connection)
        {
        }
        public void Process()
        {
            MyConnection.Process();
        }

        public override void OnStateChanged(CONNECTION_STATUS status)
        {
            if (status == CONNECTION_STATUS.Connected)
            {
                MyConnection = new DefaultUserConnection(base.Connection.Connection, INetworkManager.GetInstance().ClientProcessor);
                OnConnect?.Invoke();
            }
            else if (status == CONNECTION_STATUS.Disconnected)
            {
                OnDisconnect?.Invoke();
            }
        }

        public override void OnMessage(ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber)
        {
            NetworkLogger.Debug(NetworkLogger.CHANNEL.Default, $"{nameof(DefaultConnectionManager)}{MyConnection.Id} - Received message of size {data.Length}, num: {messageNumber}, time: {timestamp}, channel: {channel}");
            MyConnection.HandleMessage(data, timestamp, messageNumber);
        }
    }
}