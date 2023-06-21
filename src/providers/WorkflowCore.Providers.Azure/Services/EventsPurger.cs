using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Azure.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly Lazy<Container> _workflowContainer;
        public EventsPurgerOptions Options { get; }

        public EventsPurger(ICosmosClientFactory clientFactory, string dbId, CosmosDbStorageOptions cosmosDbStorageOptions, EventsPurgerOptions options)
        {
            _workflowContainer = new Lazy<Container>(() => clientFactory.GetCosmosClient()
                .GetDatabase(dbId)
                .GetContainer(cosmosDbStorageOptions.WorkflowContainerName));

            Options = options;
        }

        public async Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            var events = _workflowContainer.Value.GetItemLinqQueryable<PersistedEvent>(requestOptions: new QueryRequestOptions() { MaxItemCount = Options.BatchSize })
                .Where(x => x.EventTime < olderThanUtc && x.IsProcessed == true);

            var eventsToDelete = await events.CountAsync();

            while(eventsToDelete > 0)
            {
                using (FeedIterator<PersistedEvent> feedIterator = events.ToFeedIterator())
                { 

                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                        {
                            await _workflowContainer.Value.DeleteItemAsync<PersistedEvent>(item.id, new PartitionKey(item.id), cancellationToken: cancellationToken);
                        }
                    }
                }

                events = _workflowContainer.Value.GetItemLinqQueryable<PersistedEvent>(requestOptions: new QueryRequestOptions() { MaxItemCount = Options.BatchSize })
                    .Where(x => x.EventTime < olderThanUtc && x.IsProcessed == true);
                eventsToDelete = await events.CountAsync();
            }
        }
    }
}
