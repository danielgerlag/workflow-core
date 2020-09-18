using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Azure.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class CosmosDbPersistenceProvider : IPersistenceProvider
    {

        public const string WorkflowContainerName = "workflows";
        public const string EventContainerName = "events";
        public const string SubscriptionContainerName = "subscriptions";

        private ICosmosDbProvisioner _provisioner;
        private string _dbId;
        private CosmosClient _client;
        private Lazy<Container> _workflowContainer;
        private Lazy<Container> _eventContainer;
        private Lazy<Container> _subscriptionContainer;

        public CosmosDbPersistenceProvider(string connectionString, string dbId, ICosmosDbProvisioner provisioner)
        {
            _provisioner = provisioner;
            _dbId = dbId;
            _client = new CosmosClient(connectionString);
            _workflowContainer = new Lazy<Container>(() => _client.GetDatabase(_dbId).GetContainer(WorkflowContainerName));
            _eventContainer = new Lazy<Container>(() => _client.GetDatabase(_dbId).GetContainer(EventContainerName));
            _subscriptionContainer = new Lazy<Container>(() => _client.GetDatabase(_dbId).GetContainer(SubscriptionContainerName));
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            var existing = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId));
            
            if (existing.Resource.ExternalToken != token)
                throw new InvalidOperationException();
            existing.Resource.ExternalToken = null;
            existing.Resource.ExternalWorkerId = null;
            existing.Resource.ExternalTokenExpiry = null;

            await _subscriptionContainer.Value.ReplaceItemAsync(existing.Resource, eventSubscriptionId);
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            var result = await _eventContainer.Value.CreateItemAsync(PersistedEvent.FromInstance(newEvent));
            return result.Resource.id;
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var result = await _subscriptionContainer.Value.CreateItemAsync(PersistedSubscription.FromInstance(subscription));
            return result.Resource.id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();
            var result = await _workflowContainer.Value.CreateItemAsync(PersistedWorkflow.FromInstance(workflow));
            return result.Resource.id;
        }

        public void EnsureStoreExists()
        {
            _provisioner.Provision(_dbId).Wait();
        }

        public async Task<Event> GetEvent(string id)
        {
            var resp = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id));
            return PersistedEvent.ToInstance(resp.Resource);
        }

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            var data = _eventContainer.Value.GetItemLinqQueryable<PersistedEvent>(true)
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= asOf)
                .Select(x => x.id);
            
            return Task.FromResult(data.AsEnumerable());
        }

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            var data = _subscriptionContainer.Value.GetItemLinqQueryable<PersistedSubscription>(true)
                .FirstOrDefault(x => x.ExternalToken == null &&  x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            
            return Task.FromResult(PersistedSubscription.ToInstance(data));
        }

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var data = _eventContainer.Value.GetItemLinqQueryable<PersistedEvent>(true)
                .Where(x => !x.IsProcessed)
                .Where(x => x.EventTime <= asAt.ToUniversalTime())
                .Select(x => x.id);
            
            return Task.FromResult(data.AsEnumerable());
        }

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            var now = asAt.ToUniversalTime().Ticks;

            var data = _workflowContainer.Value.GetItemLinqQueryable<PersistedWorkflow>(true)
                .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                .Select(x => x.id);

            return Task.FromResult(data.AsEnumerable());
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            var resp = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId));
            return PersistedSubscription.ToInstance(resp.Resource);
        }

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            var data = _subscriptionContainer.Value.GetItemLinqQueryable<PersistedSubscription>(true)
                .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                .ToList()
                .Select(x => PersistedSubscription.ToInstance(x));
            return Task.FromResult(data.AsEnumerable());
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            var result = await _workflowContainer.Value.ReadItemAsync<PersistedWorkflow>(Id, new PartitionKey(Id));
            return PersistedWorkflow.ToInstance(result.Resource);
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public async Task MarkEventProcessed(string id)
        {
            var evt = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id));
            evt.Resource.IsProcessed = true;
            await _eventContainer.Value.ReplaceItemAsync(evt.Resource, id);
        }

        public async Task MarkEventUnprocessed(string id)
        {
            var evt = await _eventContainer.Value.ReadItemAsync<PersistedEvent>(id, new PartitionKey(id));
            evt.Resource.IsProcessed = false;
            await _eventContainer.Value.ReplaceItemAsync(evt.Resource, id);
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            return Task.CompletedTask;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            await _workflowContainer.Value.UpsertItemAsync(PersistedWorkflow.FromInstance(workflow));
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            var sub = await _subscriptionContainer.Value.ReadItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId));
            var existingEntity = sub.Resource;
            existingEntity.ExternalToken = token;
            existingEntity.ExternalWorkerId = workerId;
            existingEntity.ExternalTokenExpiry = expiry;
            
            await _subscriptionContainer.Value.ReplaceItemAsync(existingEntity, eventSubscriptionId);

            return true;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            await _subscriptionContainer.Value.DeleteItemAsync<PersistedSubscription>(eventSubscriptionId, new PartitionKey(eventSubscriptionId));
        }
    }
}
