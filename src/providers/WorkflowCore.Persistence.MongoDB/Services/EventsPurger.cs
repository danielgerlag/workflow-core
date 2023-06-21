using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using MongoDB.Bson;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly IMongoDatabase _database;
        private IMongoCollection<Event> Events => _database.GetCollection<Event>(MongoPersistenceProvider.EventCollectionName);

        public int BatchSize { get; }

        public EventsPurger(IMongoDatabase database, int batchSize)
        {
            _database = database;
            BatchSize = batchSize;
        }

        public async Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();

            long deletedEvents = BatchSize;
            while(deletedEvents > 0)
            {
                var events = Events
                    .Find(x => x.EventTime < olderThanUtc &&
                               x.IsProcessed == true)
                    .Limit(BatchSize)
                    .ToBsonDocument();

                var deletedResult = await Events
                    .DeleteManyAsync(events, cancellationToken);

                deletedEvents = deletedResult.DeletedCount;
            }
        }
    }
}
