using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class InMemoryQueueCache : IQueueCache, IDisposable
    {
        private readonly Timer _cycleTimer;
        private readonly HashSet<CacheItem> _items;
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;

        public InMemoryQueueCache(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InMemoryQueueCache>();
            _items = new HashSet<CacheItem>();
            var cyclePeriod = TimeSpan.FromMinutes(30);
            _cycleTimer = new Timer(o => _ = Cycle(), default, cyclePeriod, cyclePeriod);
        }

        public async Task<bool> AddOrUpdateAsync(
            CacheItem id, 
            CancellationToken cancellationToken)
        {
            await _sync.WaitAsync(cancellationToken);

            try
            {
                if (!_items.Contains(id))
                {
                    _items.Add(id);
                    return true;
                }

                CacheItem item = _items.First(i => i == id);
                var isExpired = item.IsExpired();
                if (isExpired)
                {
                    _items.Remove(id);
                    _items.Add(id);
                    return true;
                }

                return false;
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task RemoveAsync(
            CacheItem id, 
            CancellationToken cancellationToken)
        {
            await _sync.WaitAsync(cancellationToken);

            try
            {
                _items.Remove(id);
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
    }
}
