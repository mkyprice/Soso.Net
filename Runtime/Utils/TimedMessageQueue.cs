using System;
using System.Collections.Generic;
using Soso.Net.Models.Packets;
using Soso.Utils;
using Soso.Utils.Concurrency;
using System.Threading;
using UnityEngine;

namespace Soso.Net.Utils
{
	public class TimedMessageQueue
	{
		public int Count => _queue.Count;
		private readonly SosoConcurrentSortedQueue<double, NetworkMessage> _queue = new SosoConcurrentSortedQueue<double, NetworkMessage>();

		public void AddMessage(NetworkMessage message)
		{
			_queue.Add(message.Time, message);
		}

		public async Awaitable<NetworkMessage> WaitForMessageAsync(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			NetworkMessage message;
			
			while (_queue.TryDequeueFirst(out message, token) == false)
			{
				await Awaitable.NextFrameAsync(token);
			}

			return message;
		}
		
		public bool TryDequeue(out NetworkMessage message)
		{
			return _queue.TryDequeueFirst(out message);
		}

		public void Clear()
		{
			_queue.Clear();
		}
	}
}
