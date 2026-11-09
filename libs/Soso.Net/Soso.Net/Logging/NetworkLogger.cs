using System;
using Soso.Utils.Logging;

namespace Soso.Net.Logging
{
	/// <summary>
	/// TODO: DI logging system
	/// </summary>
	public static class NetworkLogger
	{
		public static LOG_LEVEL Level
		{
			get => Logger.Level;
			set => Logger.Level = value;
		}

		public static SosoLogger Logger = new SosoLogger();
		
		public static void Debug(string message, params object[]? args) => Logger.Debug(message, args);
		public static void Info(string message, params object[]? args) => Logger.Info(message, args);
		public static void Warn(string message, params object[]? args) => Logger.Warn(message, args);
		public static void Error(string message, params object[]? args) => Logger.Error(message, args);
	}
}
