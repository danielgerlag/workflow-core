using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class TransientMemoryPersistenceProvider : IPersistenceProvider
    {
        private readonly ISingletonMemoryProvider _innerService;

        public TransientMemoryPersistenceProvider(ISingletonMemoryProvider innerService)
        {
            _innerService = innerService;
        }

        public Task<string> CreateEvent(Event newEvent) => _innerService.CreateEvent(newEvent);

        public Task<string> CreateEventSubscription(EventSubscription subscription) => _innerService.CreateEventSubscription(subscription);

        public Task<string> CreateNewWorkflow(WorkflowInstance workflow) => _innerService.CreateNewWorkflow(workflow);

        public void EnsureStoreExists() => _innerService.EnsureStoreExists();

        public Task<Event> GetEvent(string id) => _innerService.GetEvent(id);

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf) => _innerService.GetEvents(eventName, eventKey, asOf);

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt) => _innerService.GetRunnableEvents(asAt);

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt) => _innerService.GetRunnableInstances(asAt);

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf) => _innerService.GetSubscriptions(eventName, eventKey, asOf);

        public Task<WorkflowInstance> GetWorkflowInstance(string Id) => _innerService.GetWorkflowInstance(Id);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids) => _innerService.GetWorkflowInstances(ids);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take) => _innerService.GetWorkflowInstances(status, type, createdFrom, createdTo, skip, take);

        public Task MarkEventProcessed(string id) => _innerService.MarkEventProcessed(id);

        public Task MarkEventUnprocessed(string id) => _innerService.MarkEventUnprocessed(id);

        public Task PersistErrors(IEnumerable<ExecutionError> errors) => _innerService.PersistErrors(errors);

        public Task PersistWorkflow(WorkflowInstance workflow) => _innerService.PersistWorkflow(workflow);

        public Task TerminateSubscription(string eventSubscriptionId) => _innerService.TerminateSubscription(eventSubscriptionId);
        public Task<EventSubscription> GetSubscription(string eventSubscriptionId) => _innerService.GetSubscription(eventSubscriptionId);

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf) => _innerService.GetFirstOpenSubscription(eventName, eventKey, asOf);

        public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry) => _innerService.SetSubscriptionToken(eventSubscriptionId, token, workerId, expiry);

        public Task ClearSubscriptionToken(string eventSubscriptionId, string token) => _innerService.ClearSubscriptionToken(eventSubscriptionId, token);
    }
}
