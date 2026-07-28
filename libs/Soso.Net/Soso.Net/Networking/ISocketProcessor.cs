using System;
using Soso.Net.Packets;

namespace Soso.Net
{
	internal interface ISocketProcessor
	{
		void OnStateChanged(SosoSocket socket, CONNECTION_STATUS status);
		void OnMessage(SosoSocket socket, int packetType, ReadOnlySpan<byte> data, int channel, long timestamp, long messageNumber);
	}
}
