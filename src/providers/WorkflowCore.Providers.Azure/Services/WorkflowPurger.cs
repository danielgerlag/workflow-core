using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Azure.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class WorkflowPurger : IWorkflowPurger
    {
        private readonly Lazy<Container> _workflowContainer;

        public WorkflowPurger(ICosmosClientFactory clientFactory, string dbId, CosmosDbStorageOptions cosmosDbStorageOptions)
        {
            _workflowContainer = new Lazy<Container>(() => clientFactory.GetCosmosClient()
                .GetDatabase(dbId)
                .GetContainer(cosmosDbStorageOptions.WorkflowContainerName));
        }

        public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            using (FeedIterator<PersistedWorkflow> feedIterator = _workflowContainer.Value.GetItemLinqQueryable<PersistedWorkflow>()
                    .Where(x => x.Status == status && x.CompleteTime < olderThanUtc)
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        await _workflowContainer.Value.DeleteItemAsync<PersistedWorkflow>(item.id, new PartitionKey(item.id), cancellationToken: cancellationToken);
                    }
                }
            }
        }
    }
}
