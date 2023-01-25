using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisQueueProvider : IQueueProvider
    {
        private readonly string _connectionString;
        private readonly string _prefix;

        private IConnectionMultiplexer _multiplexer;
        private IDatabase _redis;

        private readonly Dictionary<(QueueType, QueuePriority), string> _queues = new Dictionary<(QueueType, QueuePriority), string>
        {
            [(QueueType.Workflow, QueuePriority.Normal)] = "workflows",
            [(QueueType.Workflow, QueuePriority.High)] = "workflowshigh",
            [(QueueType.Event, QueuePriority.Normal)] = "events",
            [(QueueType.Index, QueuePriority.Normal)] = "index"
        };

        public RedisQueueProvider(string connectionString, string prefix, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _prefix = prefix;
        }

        private IEnumerable<string> GetQueuesSortedByPriority(QueueType queue)
        {
            return _queues
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        public async Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            var queueName = GetQueueName(_queues[(queue, priority)]);

            var insertResult = await _redis.ListInsertBeforeAsync(queueName, id, id);
            if (insertResult == -1 || insertResult == 0)
                await _redis.ListRightPushAsync(queueName, id, When.Always);
            else
                await _redis.ListRemoveAsync(queueName, id, 1);
        }

        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        private async Task<string> DequeueWork(string queue)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            var result = await _redis.ListLeftPopAsync(GetQueueName(queue));

            if (result.IsNull)
                return null;

            return result;
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            foreach (var q in GetQueuesSortedByPriority(queue))
            {
                var result = await DequeueWork(q);
                if (result != null)
                    return result;
            }

            return null;
        }

        public bool IsDequeueBlocking => false;

        public async Task Start()
        {
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            _redis = _multiplexer.GetDatabase();
        }

        public async Task Stop()
        {
            await _multiplexer.CloseAsync();
            _redis = null;
            _multiplexer = null;
        }

        public void Dispose()
        {
        }

        private string GetQueueName(string queue) => $"{_prefix}-{queue}";
    }
}
