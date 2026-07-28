using System;

namespace Soso.Net.Messaging.Basic
{
	public interface IMessageHandler
	{
		public void Handle(object obj);
	}
	
	public class MessageHandler<T> : IMessageHandler
	{
		public Action<T> Handler;
		
		public static MessageHandler<T> operator +(MessageHandler<T> a, Action<T> b)
		{
			a.Handler += b;
			return a;
		}
		
		public static MessageHandler<T> operator -(MessageHandler<T> a, Action<T> b)
		{
			a.Handler -= b;
			return a;
		}
		
		public void Handle(object obj)
		{
			Handler?.Invoke((T)obj);
		}
	}
}
