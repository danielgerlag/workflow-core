using System;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Squadron;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    [Collection(CollectionDefinitionNames.Squadron)]
    public class MongoQueueCacheTests
    {
        private readonly MongoResource _mongoResource;

        public MongoQueueCacheTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task GivenCacheItem_WhenAdded_ThenResultIsTrue()
        {
            // Arrange
            var database = _mongoResource.CreateDatabase();
            var queueCache = new MongoQueueCache(database);
            var cacheItem = new CacheItem(Guid.NewGuid().ToString(), DateTime.UtcNow);

            // Act
            var wasAdded = await queueCache.AddOrUpdateAsync(cacheItem, default);

            // Arrange
            wasAdded.Should().BeTrue();
            (await GetCacheItem(database)).Should().Be(cacheItem);
        }

        [Fact]
        public async Task GivenCacheItem_WhenRemoved_ThenIsNull()
        {
            // Arrange
            var database = _mongoResource.CreateDatabase();
            var queueCache = new MongoQueueCache(database);
            var cacheItem = new CacheItem(Guid.NewGuid().ToString(), DateTime.UtcNow);
            await AddCacheItem(database, cacheItem);

            // Act
            await queueCache.RemoveAsync(cacheItem, default);

            // Arrange
            (await GetCacheItem(database)).Should().BeNull();
        }

        private async Task AddCacheItem(IMongoDatabase database, CacheItem item)
        {
            var cacheItems = database.GetCollection<CacheItem>(MongoQueueCache.CollectionName);

            await cacheItems.InsertOneAsync(item);
        }

        private async Task<CacheItem?> GetCacheItem(IMongoDatabase database)
        {
            var cacheItems = database.GetCollection<BsonDocument>(MongoQueueCache.CollectionName);

            var bsonDocument = await cacheItems
                .Find(Builders<BsonDocument>.Filter.Empty)
                .FirstOrDefaultAsync();

            if (bsonDocument == null)
            {
                return null;
            }

            var id = bsonDocument["_id"].AsString;
            var timestamp = bsonDocument[nameof(CacheItem.Timestamp)].ToUniversalTime();
            return new CacheItem(id, timestamp);
        }
    }
}