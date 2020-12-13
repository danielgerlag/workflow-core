using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoQueueCache : IQueueCache
    {
        internal const string WorkflowCollectionName = "wfc.queueCache";
        private readonly IMongoDatabase _database;

        public MongoQueueCache(IMongoDatabase database)
        {
            _database = database;
        }

        public Task<bool> Add(CacheItem id)
        {
            throw new NotImplementedException();
        }

        public Task Remove(CacheItem id)
        {
            throw new NotImplementedException();
        }
    }
}