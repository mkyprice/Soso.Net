using Soso.Net.Packets;
using Soso.Net.Transports;
using System;
using System.Buffers;
using K4os.Compression.LZ4.Streams;
using Soso.Net.Logging;
using Soso.Net.Stream;
using Soso.Serialization.Binary;

namespace Soso.Net
{
	public class SosoSocket
	{
		public readonly SocketState State;
		public ISocketConnection Socket => _socket;
		
		private readonly ISocketConnection _socket;
		private long _messageNumber = 0;
		
		private readonly ISocketProcessor _processor;
		private readonly ByteBuffer _receiveBuffer;
		private readonly ByteBuffer _sendBuffer;
		
		internal SosoSocket(ISocketConnection connection, ISocketProcessor processor)
		{
			State = new SocketState(Guid.NewGuid().ToString().GetHashCode());
			_processor = processor;
			_socket = connection;
			_receiveBuffer = new ByteBuffer(SosoNetwork.BUFFER_SIZE);
			_sendBuffer = new ByteBuffer(SosoNetwork.BUFFER_SIZE);
			_socket.SetHandler(_receiveBuffer);
			SosoNetwork.AddSocket(this);
		}

		public override int GetHashCode()
		{
			return (State != null ? State.GetHashCode() : 0);
		}

		public void Process()
		{
			if (_socket.Poll() == false)
			{
				NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Shutting down {id}", GetConnectionId());
				SetState(CONNECTION_STATUS.Disconnected);
				SosoNetwork.RemoveSocket(this);
				return;
			}
			_socket.Process();
			SendPacketsFromWriter();
			ReadPacketsFromReader();
		}

		public void Send(ReadOnlySpan<byte> data, int channel, int packetType)
		{
			Packet packet = Packet.Create(packetType, channel, data);

			ByteWriter writer = new ByteWriter(_sendBuffer.Buffer);
			writer.Position = _sendBuffer.Position;
			int startPos = writer.Position;
			packet.Write(ref writer);
			int endPos = writer.Position;
			int length = endPos - startPos;
			_sendBuffer.Receive(length);
		}

		public ulong GetConnectionId() => State.Id;
		
		public void Shutdown()
		{
			try
			{
				SetState(CONNECTION_STATUS.Disconnected);
				_socket.Shutdown();
			}
			catch (Exception e)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Shutdown failed with error: {e.Message}", e);
			}
			SosoNetwork.RemoveSocket(this);
		}

		public override bool Equals(object obj)
		{
			return obj is SosoSocket other && this.Equals(other);
		}

		internal void SetState(CONNECTION_STATUS state)
		{
			if (State.Status == state)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "State is already set to {state}", state);
				return;
			}
			State.SetState(state);
			_processor.OnStateChanged(this, state);
		}

		protected bool Equals(SosoSocket other)
		{
			return Equals(State.SocketId, other.State.SocketId);
		}

		private byte[] _encodeBuffer = new byte[SosoNetwork.BUFFER_SIZE];
		private void SendPacketsFromWriter()
		{
			if (_sendBuffer.Count <= 0)
			{
				return;
			}

			if (SosoNetwork.UseCompression)
			{
				int length = LZ4Frame.Encode(new Span<byte>(_sendBuffer.Buffer, 0, _sendBuffer.Count), _encodeBuffer, SosoNetwork.CompressionSettings);
				
				_socket.Send(_encodeBuffer, 0, length);
			}
			else
			{
				_socket.Send(_sendBuffer.Buffer, 0, _sendBuffer.Count);
			}
			_sendBuffer.Flush();
		}


		private ArrayBufferWriter<byte> _decodeBuffers = new ArrayBufferWriter<byte>();
		private void ReadPacketsFromReader()
		{
			if (_receiveBuffer.Position <= 0)
			{
				return;
			}

			ByteReader reader;
			
			if (SosoNetwork.UseCompression)
			{
				var result = LZ4Frame.Decode(_receiveBuffer.Buffer.AsSpan(0, _receiveBuffer.Count), _decodeBuffers);

				reader = new ByteReader(result.WrittenSpan);
			}
			else
			{
				reader = new ByteReader(_receiveBuffer.Buffer.AsSpan(0, _receiveBuffer.Count));
			}
			while ((reader.Count - reader.Position) > PacketHeader.HEADER_SIZE)
			{
				PacketHeader header = PacketHeader.Peek(ref reader);
				if (reader.Count - reader.Position < header.Length + PacketHeader.HEADER_SIZE)
				{
					NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Packet size was too small. Expected: {header.Length}. Buffer: {buff}", header.Length, reader.Count - reader.Position);
					break;
				}

				reader.Skip(PacketHeader.HEADER_SIZE);
					
				_messageNumber++;
				Packet packet = Packet.Read(ref reader, header, _messageNumber);
				_processor.OnMessage(this, packet.Header.PacketType, packet.Data, packet.Channel, packet.Timestamp, _messageNumber);
			}
			
			_receiveBuffer.Flush();
			_decodeBuffers.Clear();
		}
	}
}
