using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class CosmosDbPersistenceProvider : IPersistenceProvider
    {

        private CosmosClient _client;
        private Database _db;
        private Container _workflowContainer;
        private Container _eventContainer;
        private Container _subscriptionContainer;

        public CosmosDbPersistenceProvider()
        {
            //new CosmosClient()
            //_workflowContainer = _db.CreateContainerIfNotExistsAsync()
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            var existing = await _subscriptionContainer.ReadItemAsync<EventSubscription>(eventSubscriptionId, PartitionKey.None);
            
            if (existing.Resource.ExternalToken != token)
                throw new InvalidOperationException();
            existing.Resource.ExternalToken = null;
            existing.Resource.ExternalWorkerId = null;
            existing.Resource.ExternalTokenExpiry = null;

            await _subscriptionContainer.ReplaceItemAsync(existing.Resource, eventSubscriptionId);
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            var result = await _eventContainer.CreateItemAsync(newEvent);
            return result.Resource.Id;
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var result = await _subscriptionContainer.CreateItemAsync(subscription);
            return result.Resource.Id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();
            var result = await _workflowContainer.CreateItemAsync(workflow);
            return result.Resource.Id;
        }

        public void EnsureStoreExists()
        {
            throw new NotImplementedException();
        }

        public async Task<Event> GetEvent(string id)
        {
            var resp = await _eventContainer.ReadItemAsync<Event>(id, PartitionKey.None);
            return resp.Resource;
        }

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            var data = _eventContainer.GetItemLinqQueryable<Event>()
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= asOf)
                .Select(x => x.Id);
            
            return Task.FromResult(data.AsEnumerable());
        }

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            var data = _subscriptionContainer.GetItemLinqQueryable<EventSubscription>()
                .FirstOrDefault(x => x.ExternalToken == null &&  x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            
            return Task.FromResult(data);
        }

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var data = _eventContainer.GetItemLinqQueryable<Event>()
                .Where(x => !x.IsProcessed)
                .Where(x => x.EventTime <= asAt.ToUniversalTime())
                .Select(x => x.Id);
            
            return Task.FromResult(data.AsEnumerable());
        }

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            var now = asAt.ToUniversalTime().Ticks;

            var data = _workflowContainer.GetItemLinqQueryable<WorkflowInstance>()
                .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                .Select(x => x.Id);

            return Task.FromResult(data.AsEnumerable());
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            var resp = await _subscriptionContainer.ReadItemAsync<EventSubscription>(eventSubscriptionId, PartitionKey.None);
            return resp.Resource;
        }

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            var data = _subscriptionContainer.GetItemLinqQueryable<EventSubscription>()
                .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            return Task.FromResult(data.AsEnumerable());
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            var result = await _workflowContainer.ReadItemAsync<WorkflowInstance>(Id, new PartitionKey(Id));
            return result.Resource;
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
            var evt = await _eventContainer.ReadItemAsync<Event>(id, PartitionKey.None);
            evt.Resource.IsProcessed = true;
            await _eventContainer.ReplaceItemAsync(evt.Resource, id);
        }

        public async Task MarkEventUnprocessed(string id)
        {
            var evt = await _eventContainer.ReadItemAsync<Event>(id, PartitionKey.None);
            evt.Resource.IsProcessed = false;
            await _eventContainer.ReplaceItemAsync(evt.Resource, id);
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            return Task.CompletedTask;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            await _workflowContainer.UpsertItemAsync(workflow);
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            var sub = await _subscriptionContainer.ReadItemAsync<EventSubscription>(eventSubscriptionId, PartitionKey.None);
            var existingEntity = sub.Resource;
            existingEntity.ExternalToken = token;
            existingEntity.ExternalWorkerId = workerId;
            existingEntity.ExternalTokenExpiry = expiry;
            
            await _subscriptionContainer.ReplaceItemAsync(existingEntity, eventSubscriptionId);

            return true;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            await _subscriptionContainer.DeleteItemAsync<EventSubscription>(eventSubscriptionId, PartitionKey.None);
        }
    }
}
