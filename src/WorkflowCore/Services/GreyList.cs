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
        private readonly IDateTimeProvider _dateTimeProvider;
        private const int CYCLE_TIME = 30;
        private const int TTL = 5;

        public GreyList(ILoggerFactory loggerFactory, IDateTimeProvider dateTimeProvider)
        {
            _logger = loggerFactory.CreateLogger<GreyList>();
            _dateTimeProvider = dateTimeProvider;
            _list = new ConcurrentDictionary<string, DateTime>();
            _cycleTimer = new Timer(new TimerCallback(Cycle), null, TimeSpan.FromMinutes(CYCLE_TIME), TimeSpan.FromMinutes(CYCLE_TIME));
        }

        public void Add(string id)
        {
            _list.AddOrUpdate(id, _dateTimeProvider.Now, (key, val) => _dateTimeProvider.Now);
        }

        public bool Contains(string id)
        {
            if (!_list.TryGetValue(id, out var start))
                return false;

            var result = start > (_dateTimeProvider.Now.AddMinutes(-1 * TTL));

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
