using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class InMemoryQueueCache : IQueueCache, IDisposable
    {
        private readonly Timer _cycleTimer;
        private readonly ConcurrentDictionary<string, DateTime> _list;
        private readonly ILogger _logger;
        private const int CYCLE_TIME = 30;
        private const int TTL = 5;

        public InMemoryQueueCache(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InMemoryQueueCache>();
            _list = new ConcurrentDictionary<string, DateTime>();
            _cycleTimer = new Timer(new TimerCallback(Cycle), null, TimeSpan.FromMinutes(CYCLE_TIME), TimeSpan.FromMinutes(CYCLE_TIME));
        }

        public Task Add(string id)
        {
            _list.AddOrUpdate(id, DateTime.Now, (key, val) => DateTime.Now);
            return Task.CompletedTask;
        }

        public Task<bool> Contains(string id)
        {
            if (!_list.TryGetValue(id, out var start))
                return Task.FromResult(true);

            var result = start > (DateTime.Now.AddMinutes(-1 * TTL));

            if (!result)
                _list.TryRemove(id, out var _);

            return Task.FromResult(result);
        }

        private void Cycle(object target)
        {
            try
            {
                _list.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public void Dispose()
        {
            _cycleTimer.Dispose();
        }

        public Task Remove(string id)
        {
            _list.TryRemove(id, out var _);
            return Task.CompletedTask;
        }
    }
}
