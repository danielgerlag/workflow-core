using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Redis.Services
{
    /// <summary>
    /// Redis sorted-set <see cref="IQueueProvider"/>. Pending ids are unique per queue.
    /// </summary>
    /// <remarks>
    /// Compatibility:
    /// <list type="bullet">
    /// <item>Queue keys are the same names as the previous LIST implementation
    /// ({prefix}-workflows|events|index) but the Redis value type is now ZSET. LIST and ZSET
    /// cannot share a key. Mixed old/new providers on those keys are not supported.</item>
    /// <item><see cref="Start"/> atomically migrates an existing LIST (first occurrence kept,
    /// FIFO head = lowest score) so a coordinated cutover does not need a manual flush.</item>
    /// <item>Enqueue is <c>ZADD NX</c> with a Redis <c>TIME</c> microsecond score. Re-queue
    /// while pending is a no-op (score and position are unchanged). After dequeue, the same
    /// id may be added again with a new score.</item>
    /// <item>Dequeue is atomic Lua <c>ZRANGE</c> + <c>ZREM</c> (ZPOPMIN equivalent) so Redis
    /// 5+ is not required. Native <c>ZPOPMIN</c> itself needs Redis 5+.</item>
    /// </list>
    /// </remarks>
    public class RedisQueueProvider : IQueueProvider
    {
        /// <summary>
        /// Unique enqueue: ZADD NX with Redis server TIME as the sort score (µs since epoch).
        /// Single-key so it is Redis Cluster safe. TIME is the Redis process clock, not the
        /// client, so application-node clock skew does not affect FIFO.
        /// </summary>
        private const string UniqueEnqueueScript = @"
local t = redis.call('TIME')
local score = tonumber(t[1]) * 1000000 + tonumber(t[2])
return redis.call('ZADD', KEYS[1], 'NX', score, ARGV[1])
";

        /// <summary>
        /// Atomic pop of the lowest-score member (FIFO). Equivalent to ZPOPMIN; implemented
        /// with ZRANGE+ZREM so Redis 3/4 still work.
        /// </summary>
        private const string DequeueScript = @"
local items = redis.call('ZRANGE', KEYS[1], 0, 0)
if #items == 0 then
  return nil
end
redis.call('ZREM', KEYS[1], items[1])
return items[1]
";

        /// <summary>
        /// One-time LIST → ZSET conversion for coordinated upgrades. First LIST occurrence
        /// wins (ZADD NX); later duplicates are dropped. Migrated scores are 1..n so they
        /// dequeue before any later TIME-based enqueue.
        /// </summary>
        private const string MigrateListScript = @"
local probe = redis.pcall('LRANGE', KEYS[1], 0, -1)
if type(probe) == 'table' and probe.err then
  return 0
end
if #probe == 0 then
  return 0
end
redis.call('DEL', KEYS[1])
local n = 0
for i, item in ipairs(probe) do
  if redis.call('ZADD', KEYS[1], 'NX', i, item) == 1 then
    n = n + 1
  end
end
return n
";

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

            await _redis.ScriptEvaluateAsync(
                UniqueEnqueueScript,
                new RedisKey[] { GetQueueName(queue) },
                new RedisValue[] { id });
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            var result = await _redis.ScriptEvaluateAsync(
                DequeueScript,
                new RedisKey[] { GetQueueName(queue) });

            if (result.IsNull)
                return null;

            return (string)result;
        }

        public bool IsDequeueBlocking => false;

        public async Task Start()
        {
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            _redis = _multiplexer.GetDatabase();

            foreach (var queue in _queues.Keys)
                await MigrateListIfNeeded(queue);
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

        private async Task MigrateListIfNeeded(QueueType queue)
        {
            var key = GetQueueName(queue);
            var migrated = (int)(RedisValue)await _redis.ScriptEvaluateAsync(
                MigrateListScript,
                new RedisKey[] { key });

            if (migrated > 0)
                _logger.LogInformation("Migrated {Count} pending item(s) from LIST to ZSET on {QueueKey}", migrated, key);
        }
    }
}
