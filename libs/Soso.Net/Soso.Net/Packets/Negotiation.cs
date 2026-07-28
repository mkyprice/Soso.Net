using Soso.Net.Stream;
using System;

namespace Soso.Net.Packets
{
	public class Negotiation
	{
		public ulong Id;
		public bool Accepted;

		public static Negotiation FromBytes(ReadOnlySpan<byte> bytes)
		{
			ulong id = BitConverter.ToUInt64(bytes);
			return new Negotiation()
			{
				Id = id,
				Accepted = id != 0,
			};
		}

		public Span<byte> ToBytes()
		{
			return BitConverter.GetBytes(Id);
		}
	}
}
