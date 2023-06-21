using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Azure.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class EventsPurger : IEventsPurger
    {
        private readonly Lazy<Container> _workflowContainer;

        public EventsPurger(ICosmosClientFactory clientFactory, string dbId, CosmosDbStorageOptions cosmosDbStorageOptions)
        {
            _workflowContainer = new Lazy<Container>(() => clientFactory.GetCosmosClient()
                .GetDatabase(dbId)
                .GetContainer(cosmosDbStorageOptions.WorkflowContainerName));
        }

        public async Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            using (FeedIterator<PersistedEvent> feedIterator = _workflowContainer.Value.GetItemLinqQueryable<PersistedEvent>()
                    .Where(x => x.EventTime < olderThanUtc && x.IsProcessed == true)
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        await _workflowContainer.Value.DeleteItemAsync<PersistedEvent>(item.id, new PartitionKey(item.id), cancellationToken: cancellationToken);
                    }
                }
            }
        }
    }
}
