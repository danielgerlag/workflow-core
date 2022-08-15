using System;
using System.Collections.Generic;
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
    public class CosmosDbPersistenceProvider : IPersistenceProvider
    {
        private readonly ICosmosDbProvisioner _provisioner;
        private readonly string _dbId;
        private readonly ICosmosClientFactory _clientFactory;
        private readonly Lazy<Container> _workflowContainer;
        private readonly Lazy<Container> _eventContainer;
        private readonly Lazy<Container> _subscriptionContainer;

        public CosmosDbPersistenceProvider(
            ICosmosClientFactory clientFactory,
            string dbId,
            ICosmosDbProvisioner provisioner,
            CosmosDbStorageOptions cosmosDbStorageOptions)
        {
            _provisioner = provisioner;
            _dbId = dbId;
            _clientFactory = clientFactory;
            _workflowContainer = new Lazy<Container>(() => _clientFactory.GetCosmosClient().GetDatabase(_dbId).GetContainer(cosmosDbStorageOptions.WorkflowContainerName));
            _eventContainer = new Lazy<Container>(() => _clientFactory.GetCosmosClient().GetDatabase(_dbId).GetContainer(cosmosDbStorageOptions.EventContainerName));
            _subscriptionContainer = new Lazy<Container>(() => _clientFactory.GetCosmosClient().GetDatabase(_dbId).GetContainer(cosmosDbStorageOptions.SubscriptionContainerName));
        }

        public bool SupportsScheduledCommands => false;

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default)
        {
            var existing = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId));
            
            if (existing.Resource.ExternalToken != token)
                throw new InvalidOperationException();
            existing.Resource.ExternalToken = null;
            existing.Resource.ExternalWorkerId = null;
            existing.Resource.ExternalTokenExpiry = null;

            await _subscriptionContainer.Value.ReplaceItemAsync(existing.Resource, eventSubscriptionId, cancellationToken: cancellationToken);
        }

        public async Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            var result = await _eventContainer.Value.CreateItemAsync(PersistedEvent.FromInstance(newEvent), cancellationToken: cancellationToken);
            return result.Resource.id;
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var result = await _subscriptionContainer.Value.CreateItemAsync(PersistedSubscription.FromInstance(subscription), cancellationToken: cancellationToken);
            return result.Resource.id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken)
        {
            workflow.Id = Guid.NewGuid().ToString();
            var result = await _workflowContainer.Value.CreateItemAsync(PersistedWorkflow.FromInstance(workflow), cancellationToken: cancellationToken);
            return result.Resource.id;
        }

        public void EnsureStoreExists()
        {
            _provisioner.Provision(_dbId).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<Event> GetEvent(string id, CancellationToken cancellationToken)
        {
            var resp = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return PersistedEvent.ToInstance(resp.Resource);
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken)
        {
            var events = new List<string>();
            using (FeedIterator<PersistedEvent> feedIterator = _eventContainer.Value.GetItemLinqQueryable<PersistedEvent>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        events.Add(item.id);
                    }
                }
            }

            return events;
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken)
        {
            EventSubscription eventSubscription = null;
            using (FeedIterator<PersistedSubscription> feedIterator = _subscriptionContainer.Value.GetItemLinqQueryable<PersistedSubscription>()
                    .Where(x => x.ExternalToken == null && x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults && eventSubscription == null)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        eventSubscription = PersistedSubscription.ToInstance(item);
                    }
                }
            }

            return eventSubscription;
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken)
        {
            var events = new List<string>();
            using (FeedIterator<PersistedEvent> feedIterator = _eventContainer.Value.GetItemLinqQueryable<PersistedEvent>()
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= asAt.ToUniversalTime())
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        events.Add(item.id);
                    }
                }
            }

            return events;
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken)
        {
            var now = asAt.ToUniversalTime().Ticks;

            var instances = new List<string>();
            using (FeedIterator<PersistedWorkflow> feedIterator = _workflowContainer.Value.GetItemLinqQueryable<PersistedWorkflow>()
                    .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        instances.Add(item.id);
                    }
                }
            }

            return instances;
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken)
        {
            var resp = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId), cancellationToken: cancellationToken);
            return PersistedSubscription.ToInstance(resp.Resource);
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken)
        {
            var subscriptions = new List<EventSubscription>();
            using (FeedIterator<PersistedSubscription> feedIterator = _subscriptionContainer.Value.GetItemLinqQueryable<PersistedSubscription>()
                .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                    .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync(cancellationToken))
                    {
                        subscriptions.Add(PersistedSubscription.ToInstance(item));
                    }
                }
            }

            return subscriptions;
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id, CancellationToken cancellationToken)
        {
            var result = await _workflowContainer.Value.ReadItemAsync<PersistedWorkflow>(Id, new PartitionKey(Id), cancellationToken: cancellationToken);
            return PersistedWorkflow.ToInstance(result.Resource);
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task MarkEventProcessed(string id, CancellationToken cancellationToken)
        {
            var evt = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            evt.Resource.IsProcessed = true;
            await _eventContainer.Value.ReplaceItemAsync(evt.Resource, id, cancellationToken: cancellationToken);
        }

        public async Task MarkEventUnprocessed(string id, CancellationToken cancellationToken)
        {
            var evt = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            evt.Resource.IsProcessed = false;
            await _eventContainer.Value.ReplaceItemAsync(evt.Resource, id, cancellationToken: cancellationToken);
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken _ = default)
        {
            return Task.CompletedTask;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken)
        {
            await _workflowContainer.Value.UpsertItemAsync(PersistedWorkflow.FromInstance(workflow), cancellationToken: cancellationToken);
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
        {
            await PersistWorkflow(workflow, cancellationToken);

            foreach(var subscription in subscriptions)
            {
                await CreateEventSubscription(subscription, cancellationToken);
            }
        }

        public Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ScheduleCommand(ScheduledCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken)
        {
            var sub = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId), cancellationToken: cancellationToken);
            var existingEntity = sub.Resource;
            existingEntity.ExternalToken = token;
            existingEntity.ExternalWorkerId = workerId;
            existingEntity.ExternalTokenExpiry = expiry;
            
            await _subscriptionContainer.Value.ReplaceItemAsync(existingEntity, eventSubscriptionId, cancellationToken: cancellationToken);

            return true;
        }

        public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken)
        {
            await _subscriptionContainer.Value.DeleteItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId), cancellationToken: cancellationToken);
        }
    }
}
