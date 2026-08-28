using System;
using System.Collections.Generic;
using Soso.Net.Messaging;
using Soso.Net.Models;
using Soso.Serialization;
using Soso.Serialization.Binary;

namespace Soso.Net.Transports.Default
{
    public class DefaultUserConnection : IUserConnection
    {
        public ulong Id => Connection.GetConnectionId();
        public SosoSocket Connection;
        
        private readonly MessageProcessorManager _messageProcessor;
        
        private byte[] _sendBuffer = new byte[1 * 1024 * 1024];

        private readonly Queue<QueuedMessage> _receiveQueue = new Queue<QueuedMessage>();
        private readonly Queue<QueuedMessage> _sendQueue = new Queue<QueuedMessage>();
        private readonly struct QueuedMessage
        {
            public readonly object Data;
            public readonly int Channel;
            public readonly long Time;
            public readonly long MessageNum;

            public QueuedMessage(object data, int channel, long time, long messageNum)
            {
                Data = data;
                Channel = channel;
                Time = time;
                MessageNum = messageNum;
            }
        }

        public DefaultUserConnection(SosoSocket socketConnection, MessageProcessorManager messageProcessor = null)
        {
            Connection = socketConnection;
            _messageProcessor = messageProcessor ?? new MessageProcessorManager();
        }

        public void Process()
        {
            if (_receiveQueue.Count > 0)
            {
                long currentTime = DateTime.UtcNow.Ticks;
                double lagSimulationSecs = (DefaultNetworkManager.GetInstance() as DefaultNetworkManager)?.LagSimulationSeconds ?? 0;
                long lagSimulationTicks = TimeSpan.FromSeconds(lagSimulationSecs).Ticks;
                long randomSimulation = (long)UnityEngine.Random.Range(-lagSimulationTicks / 2f, lagSimulationTicks / 2f);
                lagSimulationTicks += randomSimulation;
                while (_receiveQueue.TryPeek(out var next) && next.Time + lagSimulationTicks <= currentTime)
                {
                    next = _receiveQueue.Dequeue();
                    _messageProcessor.Process(this, next.Data, next.MessageNum, next.Time, next.Channel);
                }
            }
            
            if (_sendQueue.Count > 0)
            {
                long currentTime = DateTime.UtcNow.Ticks;
                double lagSimulationSecs = (DefaultNetworkManager.GetInstance() as DefaultNetworkManager)?.LagSimulationSeconds ?? 0;
                long lagSimulationTicks = TimeSpan.FromSeconds(lagSimulationSecs).Ticks;
                long randomSimulation = (long)UnityEngine.Random.Range(-lagSimulationTicks / 2f, lagSimulationTicks / 2f);
                lagSimulationTicks += randomSimulation;
                while (_sendQueue.TryPeek(out var next) && next.Time + lagSimulationTicks <= currentTime)
                {
                    next = _sendQueue.Dequeue();
                
                    ByteWriter writer = new ByteWriter(_sendBuffer);

                    writer.Write(next.Channel);
            
                    SosoSerializer.Serialize(ref writer, next.Data, SerializationFlags.EmbedType);

                    int length = writer.Position;

                    Connection.Send(new Span<byte>(_sendBuffer).Slice(0, length), 0, length);
                }
            }
        }

        public void Send<T>(T data, int channel, SOSO_SEND_TYPE sendType)
        {
            _sendQueue.Enqueue(new QueuedMessage(data, channel, DateTime.UtcNow.Ticks, 0));
        }
        
        public void HandleMessage(ReadOnlySpan<byte> bytes, long recvTime, long messageNum)
        {
            ByteReader reader = new ByteReader(bytes);
            int channel = reader.ReadInt();
            
            object obj = SosoSerializer.Deserialize(ref reader);

            var message = new QueuedMessage(obj, channel, recvTime, messageNum);

            // Enqueue message
            _receiveQueue.Enqueue(message);
            // _messageProcessor.Process(this, obj, messageNum, recvTime, channel);
        }
    }
}