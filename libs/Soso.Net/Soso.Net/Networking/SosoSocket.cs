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
		
		private const int COMPRESSED_FRAME_PREFIX_SIZE = sizeof(int);
		private const int PACKET_METADATA_SIZE = PacketHeader.HEADER_SIZE + sizeof(ushort) + sizeof(long);
		
		private readonly ISocketConnection _socket;
		private long _messageNumber = 0;
		
		private readonly ISocketProcessor _processor;
		private readonly ByteBuffer _receiveBuffer;
		private readonly ByteBuffer _sendBuffer;
		private ArrayBufferWriter<byte> _decodeBuffers = new ArrayBufferWriter<byte>();
		
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
				NetworkLogger.Info("Shutting down {id}", GetConnectionId());
				SetState(CONNECTION_STATUS.Disconnected);
				SosoNetwork.RemoveSocket(this);
				return;
			}
			_socket.Process();
			SendPacketsFromWriter();
			ReadPacketsFromReader();
		}

		public void Send(ReadOnlySpan<byte> data, ushort channel, int packetType)
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
				NetworkLogger.Error("Shutdown failed with error: {e.Message}", e);
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
				NetworkLogger.Warn("State is already set to {state}", state);
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
				int compressedLength = LZ4Frame.Encode(
					new Span<byte>(_sendBuffer.Buffer, 0, _sendBuffer.Count), 
					new Span<byte>(_encodeBuffer, COMPRESSED_FRAME_PREFIX_SIZE, _encodeBuffer.Length - COMPRESSED_FRAME_PREFIX_SIZE), 
					SosoNetwork.CompressionSettings
					);
				
				BitConverter.TryWriteBytes(new Span<byte>(_encodeBuffer, 0, COMPRESSED_FRAME_PREFIX_SIZE), compressedLength);
				
				_socket.Send(_encodeBuffer, 0, compressedLength + COMPRESSED_FRAME_PREFIX_SIZE);
			}
			else
			{
				_socket.Send(_sendBuffer.Buffer, 0, _sendBuffer.Count);
			}
			_sendBuffer.Flush();
		}


		private void ReadPacketsFromReader()
		{
			if (_receiveBuffer.Count <= 0)
			{
				return;
			}

			if (SosoNetwork.UseCompression)
			{
				int offset = 0;
				while (_receiveBuffer.Count - offset >= COMPRESSED_FRAME_PREFIX_SIZE)
				{
					int frameLength = BitConverter.ToInt32(_receiveBuffer.Buffer, offset);
					if (frameLength <= 0 || frameLength > SosoNetwork.BUFFER_SIZE)
					{
						NetworkLogger.Error("Invalid compressed frame length {length}. Flushing receive buffer.", frameLength);
						_receiveBuffer.Flush();
						_decodeBuffers.Clear();
						return;
					}

					if (_receiveBuffer.Count - offset < COMPRESSED_FRAME_PREFIX_SIZE + frameLength)
					{
						// Partial frame. Wait for remaining bytes on next socket read
						break;
					}

					var compressedFrame = _receiveBuffer.Buffer.AsSpan(offset + COMPRESSED_FRAME_PREFIX_SIZE, frameLength);
					LZ4Frame.Decode(compressedFrame, _decodeBuffers);

					offset += COMPRESSED_FRAME_PREFIX_SIZE + frameLength;
				}

				if (_decodeBuffers.WrittenCount > 0)
				{
					ByteReader reader = new ByteReader(_decodeBuffers.WrittenSpan);
					ReadPackets(ref reader);
					_decodeBuffers.Clear();
				}

				// Preserve trailing partial frame bytes
				_receiveBuffer.Consume(offset);
			}
			else
			{
				ByteReader reader = new ByteReader(_receiveBuffer.Buffer.AsSpan(0, _receiveBuffer.Count));
				int consumed = ReadPackets(ref reader);
				_receiveBuffer.Consume(consumed);
			}
		}
		
		private int ReadPackets(ref ByteReader reader)
		{
			while ((reader.Count - reader.Position) >= PACKET_METADATA_SIZE)
			{
				PacketHeader header = PacketHeader.Peek(ref reader);
				int fullPacketSize = PACKET_METADATA_SIZE + header.Length;

				if (reader.Count - reader.Position < fullPacketSize)
				{
					NetworkLogger.Warn("Packet size was too small. Expected: {expected}. Buffer: {buff}", fullPacketSize, reader.Count - reader.Position);
					break;
				}

				reader.Skip(PacketHeader.HEADER_SIZE);

				_messageNumber++;
				Packet packet = Packet.Read(ref reader, header, _messageNumber);
				_processor.OnMessage(this, packet.Header.PacketType, packet.Data, packet.Channel, packet.Timestamp, _messageNumber);
			}

			return reader.Position;
		}
	}
}
