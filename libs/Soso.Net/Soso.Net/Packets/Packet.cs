using Soso.Net.Stream;
using System;
using Soso.Serialization.Binary;

namespace Soso.Net.Packets
{
	public ref struct Packet
	{
		public readonly PacketHeader Header;
		public readonly int Channel;
		public readonly long Timestamp;
		public readonly long MessageNumber; // Do not serialize
		public readonly ReadOnlySpan<byte> Data;

		public Packet(PacketHeader header, int channel, long timestamp, long messageNumber, ReadOnlySpan<byte> data)
		{
			Header = header;
			Channel = channel;
			Timestamp = timestamp;
			MessageNumber = messageNumber;
			Data = data;
		}

		public void Write(ref ByteWriter writer)
		{
			writer.WriteBlittable(Header);

			writer.Write(Channel);
			writer.Write(Timestamp);
			writer.Write(Data);
		}
		
		public static Packet Read(ref ByteReader reader, PacketHeader header, long messageNumber)
		{
			var channel = reader.ReadInt();
			var timestamp = reader.ReadLong();
			var data = reader.ReadSpan(header.Length);

			var dateTime = DateTime.FromBinary(timestamp);
			Packet packet = new Packet(header, channel, dateTime.Ticks, messageNumber, data);
			return packet;
		}
		
		public static Packet Create(int packetType, int channel, ReadOnlySpan<byte> data)
		{
			PacketHeader header = new PacketHeader(data.Length, packetType);
			Packet packet = new Packet(header, channel, DateTime.UtcNow.ToBinary(), 0, data);
			return packet;
		}
	}
}
