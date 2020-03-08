using System;
using System.Collections.Generic;
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
        private Container _container;

        public CosmosDbPersistenceProvider()
        {
            //new CosmosClient()
            //_client.GetContainer().
        }

        public Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateEvent(Event newEvent)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            _container.
        }

        public void EnsureStoreExists()
        {
            throw new NotImplementedException();
        }

        public Task<Event> GetEvent(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            throw new NotImplementedException();
        }

        public Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public Task MarkEventProcessed(string id)
        {
            throw new NotImplementedException();
        }

        public Task MarkEventUnprocessed(string id)
        {
            throw new NotImplementedException();
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            throw new NotImplementedException();
        }

        public Task PersistWorkflow(WorkflowInstance workflow)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            throw new NotImplementedException();
        }

        public Task TerminateSubscription(string eventSubscriptionId)
        {
            throw new NotImplementedException();
        }
    }
}
