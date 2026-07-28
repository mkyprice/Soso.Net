using System;
using Soso.Utils.Logging;

namespace Soso.Net.Logging
{
	/// <summary>
	/// TODO: DI logging system
	/// </summary>
	public static class NetworkLogger
	{
		[Flags]
		public enum CHANNEL
		{
			Default = 1,
		}

		public static LOG_LEVEL Level
		{
			get => Logger.Level;
			set => Logger.Level = value;
		}

		public static SosoLogger<CHANNEL> Logger = new SosoLogger<CHANNEL>()
		{
			ActiveChannels = CHANNEL.Default
		};
		
		public static void Debug(CHANNEL channel, string message, params object?[]? args) => Logger.Debug(channel, message, args);
		public static void Info(CHANNEL channel, string message, params object?[]? args) => Logger.Info(channel, message, args);
		public static void Warn(CHANNEL channel, string message, params object?[]? args) => Logger.Warn(channel, message, args);
		public static void Error(CHANNEL channel, string message, params object?[]? args) => Logger.Error(channel, message, args);
	}
}
