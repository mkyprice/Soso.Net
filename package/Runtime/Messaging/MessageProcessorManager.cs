using System;
using System.Collections.Generic;
using Soso.Net.Logging;
using CHANNEL = Soso.Net.Logging.NetworkLogger.CHANNEL;

namespace Soso.Net.Messaging
{
	public class MessageProcessorManager
	{
		private readonly Dictionary<Type, IMessageProcessor> _processors = new Dictionary<Type, IMessageProcessor>();

		public void Process(IUserConnection source, object message, long messageNum, long recvTime, int channel)
		{
			NetworkLogger.Debug(CHANNEL.Default, "Processing message {message} on channel: {channel}", message, channel);
			Type type = message.GetType();
			if (_processors.TryGetValue(type, out IMessageProcessor processor))
			{
				processor.Process(source, message, messageNum, recvTime, channel);
			}
			else
			{
				NetworkLogger.Error(CHANNEL.Default, "No callback for packet type {name} with data {message}", type.Name, message);
			}
		}
		
		public void Subscribe<T>(int channel, Action<T,long,long,IUserConnection> callback)
		{
			if (TryGetProcessor(out MessageProcessor<T> processor) == false)
			{
				processor = new MessageProcessor<T>();
				_processors[typeof(T)] = processor;
			}
			processor.Subscribe(channel, callback);
		}
		public void Unsubscribe<T>(int channel, Action<T,long,long,IUserConnection> callback)
		{
			if (TryGetProcessor(out MessageProcessor<T> processor) == false)
			{
				return;
			}
			processor.Unsubscribe(channel, callback);
		}

		private bool TryGetProcessor<T>(out MessageProcessor<T> processor)
		{
			if (_processors.TryGetValue(typeof(T), out IMessageProcessor ip) == false)
			{
				processor = null;
				return false;
			}
			processor = (MessageProcessor<T>)ip;
			return true;
		}
	}
}
