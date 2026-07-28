using Soso.Net.Stream;
using Soso.Serialization.Binary;

namespace Soso.Net.Packets
{
	public readonly struct PacketHeader
	{
		public const int HEADER_SIZE = sizeof(int) + sizeof(int);
		
		public readonly int Length;
		public readonly int PacketType;
		
		public PacketHeader(int length, int packetType)
		{
			Length = length;
			PacketType = packetType;
		}

		public static PacketHeader Peek(ref ByteReader reader)
		{
			int length = reader.PeekInt();
			int packetType = reader.PeekInt(sizeof(int));
			return new PacketHeader(length, packetType);
		}
	}
}
