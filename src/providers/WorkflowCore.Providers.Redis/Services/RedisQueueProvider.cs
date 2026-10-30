using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Redis.Services
{
    /// <summary>
    /// Redis <see cref="IQueueProvider"/>. Pending ids are unique per queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Storage is selected with <see cref="RedisQueueStorage"/>. The default is
    /// <see cref="RedisQueueStorage.List"/> (backward compatible). Both modes use the
    /// same key names (<c>{prefix}-workflows|events|index</c>). Do not mix modes on
    /// the same prefix: Redis cannot store LIST and ZSET on one key (<c>WRONGTYPE</c>),
    /// and mixed hosts will split or lose work.
    /// </para>
    /// <para>
    /// <see cref="RedisQueueStorage.List"/> keeps LIST keys and the LINSERT/RPUSH/LREM
    /// uniqueness algorithm, run in one Lua <c>EVAL</c> so concurrent enqueues of a
    /// missing id cannot both <c>RPUSH</c>. Dequeue is a single <c>LPOP</c>. Existing
    /// LIST deployments (including older non-Lua hosts) can roll forward without a
    /// key-type cutover.
    /// </para>
    /// <para>
    /// <see cref="RedisQueueStorage.SortedSet"/> requires Redis 3.0.2+ (<c>ZADD NX</c>;
    /// Lua 2.6+ is not enough). Scores are Redis <c>TIME</c> microseconds; same-µs
    /// members are ordered lexicographically by id, so FIFO is slightly weaker than
    /// LIST under burst enqueue. Re-queue while pending is a no-op (score and
    /// position are unchanged). After dequeue, the same id may be added again with a
    /// new score. Dequeue is atomic Lua <c>ZRANGE</c> + <c>ZREM</c> (ZPOPMIN
    /// equivalent) so Redis 5+ is not required. <see cref="Start"/> migrates an
    /// existing LIST in one Lua <c>EVAL</c> (first occurrence kept, FIFO head =
    /// lowest score); a huge backlog can briefly block Redis.
    /// </para>
    /// </remarks>
    public class RedisQueueProvider : IQueueProvider
    {
        /// <summary>
        /// Atomic unique enqueue on a LIST: same LINSERT / RPUSH / LREM sequence as
        /// the original multi-command path, but one EVAL so two concurrent misses
        /// cannot both RPUSH. LINSERT exists on Redis 2.2+; Lua on 2.6+.
        /// </summary>
        private const string ListUniqueEnqueueScript = @"
local n = redis.call('LINSERT', KEYS[1], 'BEFORE', ARGV[1], ARGV[1])
if n == -1 or n == 0 then
  redis.call('RPUSH', KEYS[1], ARGV[1])
  return 1
end
redis.call('LREM', KEYS[1], 1, ARGV[1])
return 0
";

        /// <summary>
        /// Unique enqueue on a ZSET: ZADD NX (Redis 3.0.2+) with Redis server TIME
        /// as the sort score (µs since epoch). Same-µs members sort lexicographically
        /// by id. Single-key so it is Redis Cluster safe. TIME is the Redis process
        /// clock, not the client, so application-node clock skew does not affect FIFO.
        /// </summary>
        private const string SortedSetUniqueEnqueueScript = @"
local t = redis.call('TIME')
local score = tonumber(t[1]) * 1000000 + tonumber(t[2])
return redis.call('ZADD', KEYS[1], 'NX', score, ARGV[1])
";

        /// <summary>
        /// Atomic pop of the lowest-score member (FIFO). Equivalent to ZPOPMIN;
        /// implemented with ZRANGE+ZREM so Redis 3/4 still work.
        /// </summary>
        private const string SortedSetDequeueScript = @"
local items = redis.call('ZRANGE', KEYS[1], 0, 0)
if #items == 0 then
  return nil
end
redis.call('ZREM', KEYS[1], items[1])
return items[1]
";

        /// <summary>
        /// LIST → ZSET conversion used only when <see cref="RedisQueueStorage.SortedSet"/>
        /// is selected. Loads the whole LIST into one EVAL (can briefly block Redis
        /// on a huge backlog). First LIST occurrence wins (ZADD NX); later duplicates
        /// are dropped. Migrated scores are 1..n so they dequeue before any later
        /// TIME-based enqueue.
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
        private readonly RedisQueueStorage _storage;

        private IConnectionMultiplexer _multiplexer;
        private IDatabase _redis;

        private readonly Dictionary<QueueType, string> _queues = new Dictionary<QueueType, string>
        {
            [QueueType.Workflow] = "workflows",
            [QueueType.Event] = "events",
            [QueueType.Index] = "index"
        };

        private readonly Dictionary<QueueType, bool> _enabledQueues = new Dictionary<QueueType, bool>
        {
        };

        /// <summary>
        /// Creates a Redis queue provider using <see cref="RedisQueueStorage.List"/>
        /// with every queue type enabled.
        /// </summary>
        public RedisQueueProvider(string connectionString, string prefix, ILoggerFactory logFactory)
            : this(connectionString, prefix, RedisQueueStorage.List, options: null, logFactory)
        {
        }

        /// <summary>
        /// Creates a Redis queue provider using <see cref="RedisQueueStorage.List"/>
        /// and the enable flags from <paramref name="options"/>.
        /// </summary>
        public RedisQueueProvider(string connectionString, string prefix, WorkflowOptions options, ILoggerFactory logFactory)
            : this(connectionString, prefix, RedisQueueStorage.List, options, logFactory)
        {
        }

        /// <summary>
        /// Creates a Redis queue provider with an explicit <see cref="RedisQueueStorage"/>
        /// implementation and every queue type enabled.
        /// </summary>
        public RedisQueueProvider(string connectionString, string prefix, RedisQueueStorage storage, ILoggerFactory logFactory)
            : this(connectionString, prefix, storage, options: null, logFactory)
        {
        }

        /// <summary>
        /// Creates a Redis queue provider with an explicit <see cref="RedisQueueStorage"/> implementation.
        /// </summary>
        /// <param name="connectionString">StackExchange.Redis connection string.</param>
        /// <param name="prefix">Key prefix. Do not mix storage modes on the same prefix.</param>
        /// <param name="storage">
        /// <see cref="RedisQueueStorage.List"/> (default) or
        /// <see cref="RedisQueueStorage.SortedSet"/>.
        /// </param>
        /// <param name="options">
        /// When provided, <see cref="WorkflowOptions.EnableWorkflows"/>,
        /// <see cref="WorkflowOptions.EnableEvents"/>, and
        /// <see cref="WorkflowOptions.EnableIndexes"/> control which queues are used.
        /// When null, all queue types are enabled.
        /// </param>
        /// <param name="logFactory">Logger factory.</param>
        public RedisQueueProvider(string connectionString, string prefix, RedisQueueStorage storage, WorkflowOptions options, ILoggerFactory logFactory)
        {
            if (storage != RedisQueueStorage.List && storage != RedisQueueStorage.SortedSet)
                throw new ArgumentOutOfRangeException(nameof(storage));

            _connectionString = connectionString;
            _prefix = prefix;
            _storage = storage;
            _enabledQueues[QueueType.Index] = options?.EnableIndexes ?? true;
            _enabledQueues[QueueType.Event] = options?.EnableEvents ?? true;
            _enabledQueues[QueueType.Workflow] = options?.EnableWorkflows ?? true;
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            if (!_enabledQueues[queue])
                return;

            if (_redis == null)
                throw new InvalidOperationException();

            var script = _storage == RedisQueueStorage.SortedSet
                ? SortedSetUniqueEnqueueScript
                : ListUniqueEnqueueScript;

            await _redis.ScriptEvaluateAsync(
                script,
                new RedisKey[] { GetQueueName(queue) },
                new RedisValue[] { id });
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (!_enabledQueues[queue])
                return null;

            if (_redis == null)
                throw new InvalidOperationException();

            if (_storage == RedisQueueStorage.SortedSet)
            {
                var result = await _redis.ScriptEvaluateAsync(
                    SortedSetDequeueScript,
                    new RedisKey[] { GetQueueName(queue) });

                if (result.IsNull)
                    return null;

                return (string)result;
            }

            // Single LPOP on the LIST. No companion membership key, so nothing can be
            // left stuck (or lost) if the process dies between pop and a follow-up SREM.
            var popped = await _redis.ListLeftPopAsync(GetQueueName(queue));

            if (popped.IsNull)
                return null;

            return popped;
        }

        public bool IsDequeueBlocking => false;

        public async Task Start()
        {
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            _redis = _multiplexer.GetDatabase();

            if (_storage == RedisQueueStorage.SortedSet)
            {
                foreach (var queue in _queues.Keys)
                {
                    if (_enabledQueues[queue])
                        await MigrateListIfNeeded(queue);
                }
            }
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
