using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class InMemoryQueueCache : IQueueCache, IDisposable
    {
        private readonly Timer _cycleTimer;
        private readonly Dictionary<string, DateTime> _items;
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private const int CYCLE_TIME = 30;
        private const int TTL = 5;

        public InMemoryQueueCache(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InMemoryQueueCache>();
            _items = new Dictionary<string, DateTime>();
            _cycleTimer = new Timer(o => _ = Cycle(), null, TimeSpan.FromMinutes(CYCLE_TIME), TimeSpan.FromMinutes(CYCLE_TIME));
        }

        public async Task<bool> ContainsOrAdd(string id)
        {
            await _sync.WaitAsync();

            try
            {
                if (!_items.TryGetValue(id, out var start))
                {
                    _items[id] = DateTime.Now;
                    return false;
                }

                var isValid = start > (DateTime.Now.AddMinutes(-1 * TTL));
                if (!isValid)
                {
                    _items.Remove(id);
                }

                return isValid;
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task Cycle()
        {
            await _sync.WaitAsync();

            try
            {
                _items.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _sync.Release();
            }
        }

        public void Dispose()
        {
            _cycleTimer.Dispose();
            _sync.Dispose();
        }

        public async Task Remove(string id)
        {
            await _sync.WaitAsync();

            try
            {
                _items.Remove(id);
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
