using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisQueueProvider : IQueueProvider
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _prefix;

        private IConnectionMultiplexer _multiplexer;
        private IDatabase _redis;

        private readonly Dictionary<QueueType, string> _queues = new Dictionary<QueueType, string>
        {
            [QueueType.Workflow] = "workflows",
            [QueueType.Event] = "events",
            [QueueType.Index] = "index"
        };

        public RedisQueueProvider(string connectionString, string prefix, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _prefix = prefix;
            _logger = logFactory.CreateLogger(GetType());
        }
        
        public async Task QueueWork(string id, QueueType queue)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            var queueName = GetQueueName(queue);

            var insertResult = await _redis.ListInsertBeforeAsync(queueName, id, id);
            if (insertResult == -1 || insertResult == 0)
                await _redis.ListRightPushAsync(queueName, id, When.Always);
            else
                await _redis.ListRemoveAsync(queueName, id, 1);
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            var result = await _redis.ListLeftPopAsync(GetQueueName(queue));

            if (result.IsNull)
                return null;

            return result;
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

        private string GetQueueName(QueueType queue) => $"{_prefix}-{_queues[queue]}";
    }
}
