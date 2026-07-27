using System;
using System.Collections.Generic;
using Soso.Net.Logging;

namespace Soso.Net.Messaging
{
	internal class MessageProcessor<T> : IMessageProcessor
	{
		private readonly Dictionary<int, Action<T, long, long, IUserConnection>> _callback
			= new Dictionary<int, Action<T, long, long, IUserConnection>>();

		public void Subscribe(int channel, Action<T, long, long, IUserConnection> callback)
		{
			if (_callback.TryAdd(channel, callback) == false)
			{
				_callback[channel] += callback;
			}
		}

		public void Unsubscribe(int channel, Action<T, long, long, IUserConnection> callback)
		{
			if (_callback.TryGetValue(channel, out var cb) == false)
			{
				return;
			}
			if (cb == null)
			{
				return;
			}
			cb -= callback;
			if (cb == null || cb.GetInvocationList()?.Length <= 0)
			{
				_callback.Remove(channel);
			}
		}

		void IMessageProcessor.Process(IUserConnection source, object message, long messageNum, long recvTime, int channel)
		{
			T data = (T)message;

			if (_callback.TryGetValue(channel, out var callback))
			{
				callback(data, recvTime, messageNum, source);
			}
			else
			{
				NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "No processor configured for message {msg} on channel {channel}", message, channel);
			}
		}
	}
}
