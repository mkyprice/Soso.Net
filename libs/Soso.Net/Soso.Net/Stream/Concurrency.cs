using Soso.Net.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soso.Net.Stream
{
	public class Concurrency<T>
	{
		private readonly ConcurrentContext _context;
		private volatile int _lock = 0;

		public class ConcurrentContext : IAsyncDisposable, IDisposable
		{
			public readonly T Value;
			private readonly Concurrency<T> _manager;
			private bool _isReading = false;
			internal ConcurrentContext(Concurrency<T> manager, T value)
			{
				_manager = manager;
				Value = value;
			}

			internal void GetContextAsync()
			{
				if (_isReading)
				{
					throw new Exception("Context is already being used");
				}
				_isReading = true;
			}
			
			public async ValueTask DisposeAsync()
			{
				_isReading = false;
				_manager.Unlock();
			}
			public void Dispose()
			{
				_isReading = false;
				_manager.Unlock();
			}
		}

		public Concurrency(T data)
		{
			_context = new ConcurrentContext(this, data);
		}

		public ConcurrentContext TryGetContext()
		{
			if (Interlocked.Exchange(ref _lock, 1) == 1)
			{
				return null;
			}
			_context.GetContextAsync();
			return _context;
		}

		public async Task<ConcurrentContext> GetContextAsync()
		{
			int waitCount = 0;
			while (Interlocked.Exchange(ref _lock, 1) == 1)
			{
				await Task.Delay(1);
				waitCount++;
				if (waitCount % 100 == 0)
				{
					NetworkLogger.Info(NetworkLogger.CHANNEL.Default, "Waited {waitCount} times...", waitCount);
				}
			}
			_context.GetContextAsync();
			return _context;
		}

		private void Unlock()
		{
			if (Interlocked.Exchange(ref _lock, 0) != 1)
			{
				NetworkLogger.Warn(NetworkLogger.CHANNEL.Default, "Unlocked an unlocked context");
			}
		}
	}
}
