namespace WorkflowCore.Providers.Redis.Services
{
    /// <summary>
    /// Redis data structure used by <see cref="RedisQueueProvider"/> for pending work.
    /// </summary>
    /// <remarks>
    /// Both modes use the same key names (<c>{prefix}-workflows</c>, <c>{prefix}-events</c>,
    /// <c>{prefix}-index</c>). Do not mix <see cref="List"/> and <see cref="SortedSet"/> on
    /// the same prefix: Redis cannot store both types on one key (<c>WRONGTYPE</c>), and
    /// mixed hosts will split or lose work.
    /// </remarks>
    public enum RedisQueueStorage
    {
        /// <summary>
        /// Default. Redis LIST with atomic Lua uniqueness (LINSERT / RPUSH / LREM).
        /// Rolling-upgrade friendly with existing LIST deployments.
        /// </summary>
        List = 0,

        /// <summary>
        /// Redis sorted set (ZSET) with <c>ZADD NX</c> and Redis <c>TIME</c> scores.
        /// <see cref="RedisQueueProvider.Start"/> migrates leftover LIST items in place.
        /// </summary>
        SortedSet = 1
    }
}
