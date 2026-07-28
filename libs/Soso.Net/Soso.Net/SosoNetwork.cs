using Soso.Net.Transports.TCP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Soso.Net.Logging;

namespace Soso.Net
{
	public static class SosoNetwork
	{
		public static int BUFFER_SIZE = 8 * 1024 * 1024;
		public static bool UseCompression = true;
		public static LZ4EncoderSettings CompressionSettings = new LZ4EncoderSettings()
		{
			CompressionLevel = LZ4Level.L00_FAST,
		};
		private static List<SosoSocket> _sockets = new List<SosoSocket>();

		internal static void AddSocket(SosoSocket socket)
		{
			_sockets.Add(socket);
		}

		internal static void RemoveSocket(SosoSocket socket)
		{
			_sockets.Remove(socket);
		}

		public static void Process()
		{
			for (int i = 0; i < _sockets.Count; i++)
			{
				var socket = _sockets[i];
				socket.Process();
			}
		}
		
		public static async Task<T> ConnectAsync<T>(ulong id, EndPoint ep)
			where T : SosoConnectionManager
		{
			var connection = new SosoTcpConnection();
			await connection.ConnectAsync(ep);
			T client = Activator.CreateInstance(typeof(T), id, connection) as T;

			if (client == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not create type {nameof(T)}", nameof(T));
				return null;
			}

			while (client.Connection.Status == CONNECTION_STATUS.Connecting)
			{
				await Task.Delay(1);
				Process();
			}
			
			return client;
		}

		public static T CreateListener<T>(EndPoint ep)
			where T : SosoSocketManager
		{
			var listener = new  SosoTcpListener();

			var server = Activator.CreateInstance(typeof(T), listener) as T;
			
			if (server == null)
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "Could not create type {nameof(T)}", nameof(T));
				return null;
			}
			
			listener.StartListener(ep, 100);
			
			return server;
		}
	}
}
