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
    /// Redis LIST-backed <see cref="IQueueProvider"/>. Pending ids are unique per queue.
    /// </summary>
    /// <remarks>
    /// Compatibility:
    /// <list type="bullet">
    /// <item>Uses the existing LIST keys only ({prefix}-workflows|events|index). No companion SET,
    /// so there is no extra key to name, TTL, or clean up, and no pop/SREM crash window.</item>
    /// <item>Existing LIST keys (including those that already contain duplicate entries) need no
    /// migration or flush. Pre-existing duplicates drain via <see cref="DequeueWork"/>; a new
    /// enqueue of that id does not add another occurrence.</item>
    /// <item>Rolling upgrades with mixed old/new processes share the same LIST. New processes
    /// enqueue atomically among themselves; an old process can still race its own
    /// LINSERT/RPUSH sequence and insert a duplicate until every node is upgraded.</item>
    /// <item>Re-QueueWork while an id is still pending is a no-op (the previous multi-command
    /// de-dupe intended this, but concurrent misses could both RPUSH). After dequeue, the id
    /// may be queued again.</item>
    /// </list>
    /// </remarks>
    public class RedisQueueProvider : IQueueProvider
    {
        /// <summary>
        /// Atomic unique enqueue: same LINSERT / RPUSH / LREM sequence as before, but one EVAL
        /// so two concurrent misses cannot both RPUSH. LINSERT exists on Redis 2.2+; Lua on 2.6+.
        /// </summary>
        private const string UniqueEnqueueScript = @"
local n = redis.call('LINSERT', KEYS[1], 'BEFORE', ARGV[1], ARGV[1])
if n == -1 or n == 0 then
  redis.call('RPUSH', KEYS[1], ARGV[1])
  return 1
end
redis.call('LREM', KEYS[1], 1, ARGV[1])
return 0
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

            // Single LPOP on the LIST. No companion membership key, so nothing can be left
            // stuck (or lost) if the process dies between pop and a follow-up SREM.
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
