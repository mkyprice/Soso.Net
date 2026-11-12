

using System.Runtime.CompilerServices;
using Soso.Serialization.Binary;
using UnityEngine;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Read(this ref ByteReader reader, out Vector2 value)
		{
			value = new Vector2(reader.ReadFloat(), reader.ReadFloat());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this ref ByteWriter writer, Vector2 value)
		{
			writer.Write(value.x);
			writer.Write(value.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Read(this ref ByteReader reader, out Vector3 value)
		{
			value = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this ref ByteWriter writer, Vector3 value)
		{
			writer.Write(value.x);
			writer.Write(value.y);
			writer.Write(value.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Read(this ref ByteReader reader, out Quaternion value)
		{
			value = new Quaternion(
				reader.ReadFloat(), 
				reader.ReadFloat(), 
				reader.ReadFloat(), 
				reader.ReadFloat()
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this ref ByteWriter writer, Quaternion value)
		{
			writer.Write(value.x);
			writer.Write(value.y);
			writer.Write(value.z);
			writer.Write(value.w);
		}
	}
}
