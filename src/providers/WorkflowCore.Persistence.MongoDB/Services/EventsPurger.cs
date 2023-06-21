using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly IMongoDatabase _database;
        private IMongoCollection<Event> Events => _database.GetCollection<Event>(MongoPersistenceProvider.EventCollectionName);

        public EventsPurger(IMongoDatabase database)
        {
            _database = database;
        }

        public Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            return Events.DeleteManyAsync(x => x.EventTime < olderThanUtc &&
                                               x.IsProcessed == true, cancellationToken);
        }
    }
}
