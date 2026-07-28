

using Soso.Serialization.Binary;

namespace Soso.Net.Extensions
{
	public static class SerializationExtensions
	{
		public static void Read(this ref ByteReader reader, out NetworkInstanceId value)
		{
			value = (NetworkInstanceId)reader.ReadULong();
		}

		public static void Write(this ref ByteWriter writer, NetworkInstanceId value)
		{
			writer.Write(value);
		}
	}
}
