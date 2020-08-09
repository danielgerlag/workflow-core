using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class GreyList : IGreyList, IDisposable
    {
        private readonly Timer _cycleTimer;
        private readonly ConcurrentDictionary<string, DateTime> _list;
        private readonly ILogger _logger;
        private const int CYCLE_TIME = 30;
        private const int TTL = 5;

        public GreyList(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GreyList>();
            _list = new ConcurrentDictionary<string, DateTime>();
            _cycleTimer = new Timer(new TimerCallback(Cycle), null, TimeSpan.FromMinutes(CYCLE_TIME), TimeSpan.FromMinutes(CYCLE_TIME));
        }

        public void Add(string id)
        {
            _list.AddOrUpdate(id, DateTime.Now, (key, val) => DateTime.Now);
        }

        public bool Contains(string id)
        {
            if (!_list.TryGetValue(id, out var start))
                return false;

            var result = start > (DateTime.Now.AddMinutes(-1 * TTL));

            if (!result)
                _list.TryRemove(id, out var _);

            return result;
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

        public void Remove(string id)
        {
            _list.TryRemove(id, out var _);
        }
    }
}
