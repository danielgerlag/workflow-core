using Microsoft.Extensions.Logging;
using WorkflowCore.Models;
using WorkflowCore.Providers.Redis.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Redis queue provider using <see cref="RedisQueueStorage.List"/>
        /// (default, backward compatible with existing LIST keys).
        /// </summary>
        /// <param name="options">Workflow options.</param>
        /// <param name="connectionString">StackExchange.Redis connection string.</param>
        /// <param name="prefix">
        /// Queue key prefix (<c>{prefix}-workflows|events|index</c>). Do not mix
        /// <see cref="RedisQueueStorage"/> modes on the same prefix.
        /// </param>
        public static WorkflowOptions UseRedisQueues(this WorkflowOptions options, string connectionString, string prefix)
        {
            return options.UseRedisQueues(connectionString, prefix, RedisQueueStorage.List);
        }

        /// <summary>
        /// Registers the Redis queue provider with an explicit storage implementation.
        /// </summary>
        /// <param name="options">Workflow options.</param>
        /// <param name="connectionString">StackExchange.Redis connection string.</param>
        /// <param name="prefix">
        /// Queue key prefix (<c>{prefix}-workflows|events|index</c>). Do not mix
        /// <see cref="RedisQueueStorage"/> modes on the same prefix.
        /// </param>
        /// <param name="storage">
        /// <see cref="RedisQueueStorage.List"/> (default) keeps LIST keys with atomic
        /// Lua uniqueness and is rolling-upgrade friendly.
        /// <see cref="RedisQueueStorage.SortedSet"/> uses ZSET uniqueness;
        /// <see cref="RedisQueueProvider.Start"/> migrates leftover LIST items.
        /// Mixing modes on the same prefix causes <c>WRONGTYPE</c> or split work.
        /// </param>
        public static WorkflowOptions UseRedisQueues(this WorkflowOptions options, string connectionString, string prefix, RedisQueueStorage storage)
        {
            options.UseQueueProvider(sp => new RedisQueueProvider(connectionString, prefix, storage, sp.GetService<WorkflowOptions>(), sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseRedisLocking(this WorkflowOptions options, string connectionString, string prefix = null)
        {
            options.UseDistributedLockManager(sp => new RedisLockProvider(connectionString, prefix, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseRedisPersistence(this WorkflowOptions options, string connectionString, string prefix, bool deleteComplete = false)
        {
            options.UsePersistence(sp => new RedisPersistenceProvider(connectionString, prefix, deleteComplete, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseRedisEventHub(this WorkflowOptions options, string connectionString, string channel)
        {
            options.UseEventHub(sp => new RedisLifeCycleEventHub(connectionString, channel, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
