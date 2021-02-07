using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoQueueCache : IQueueCache
    {
        internal const string CollectionName = "wfc.queueCache";
        private readonly IMongoCollection<CacheItem> _cacheItems;

        public MongoQueueCache(IMongoDatabase database)
        {
            _cacheItems = database.GetCollection<CacheItem>(CollectionName);
            CreateIndexes(this);
        }

        private static bool _indexesCreated = false;
        private static void CreateIndexes(MongoQueueCache instance)
        {
            if (!_indexesCreated)
            {
                instance._cacheItems.Indexes.CreateOne(new CreateIndexModel<CacheItem>(
                    Builders<CacheItem>.IndexKeys.Ascending(x => x.Timestamp),
                    new CreateIndexOptions
                    {
                        Background = true, 
                        Name = "idx_timestamp_ttl", 
                        ExpireAfter = TimeSpan.FromMinutes(5)
                    }));

                _indexesCreated = true;
            }
        }

        public async Task<bool> AddOrUpdateAsync(
            CacheItem item, 
            CancellationToken cancellationToken)
        {
            var filter = Builders<CacheItem>.Filter.Eq(c => c.Id, item.Id);
            var options = new UpdateOptions
            {
                IsUpsert = true
            };

            await _cacheItems
                .ReplaceOneAsync(filter, item, options, cancellationToken);

            // Optimistic it will be always inserted
            // because the expired ones are removed by the TTL index.
            return true;
        }

        public async Task RemoveAsync(
            CacheItem item, 
            CancellationToken cancellationToken)
        {
            var filter = Builders<CacheItem>.Filter.Eq(c => c.Id, item.Id);

            await _cacheItems.DeleteOneAsync(filter, cancellationToken);
        }
    }
}