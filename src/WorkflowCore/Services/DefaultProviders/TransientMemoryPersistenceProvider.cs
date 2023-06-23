using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class TransientMemoryPersistenceProvider : IPersistenceProvider
    {
        private readonly ISingletonMemoryProvider _innerService;

        public bool SupportsScheduledCommands => false;

        public TransientMemoryPersistenceProvider(ISingletonMemoryProvider innerService)
        {
            _innerService = innerService;
        }

        public Task<string> CreateEvent(Event newEvent, CancellationToken _ = default) => _innerService.CreateEvent(newEvent);

        public Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken _ = default) => _innerService.CreateEventSubscription(subscription);

        public Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken _ = default) => _innerService.CreateNewWorkflow(workflow);

        public void EnsureStoreExists() => _innerService.EnsureStoreExists();

        public Task<Event> GetEvent(string id, CancellationToken _ = default) => _innerService.GetEvent(id);

        public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetEvents(eventName, eventKey, asOf);

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken _ = default) => _innerService.GetRunnableEvents(asAt);

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken _ = default) => _innerService.GetRunnableInstances(asAt);

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetSubscriptions(eventName, eventKey, asOf);

        public Task<WorkflowInstance> GetWorkflowInstance(string Id, CancellationToken _ = default) => _innerService.GetWorkflowInstance(Id);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken _ = default) => _innerService.GetWorkflowInstances(ids);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take) => _innerService.GetWorkflowInstances(status, type, createdFrom, createdTo, skip, take);

        public Task MarkEventProcessed(string id, CancellationToken _ = default) => _innerService.MarkEventProcessed(id);

        public Task MarkEventUnprocessed(string id, CancellationToken _ = default) => _innerService.MarkEventUnprocessed(id);

        public Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken _ = default) => _innerService.PersistErrors(errors);

        public Task PersistWorkflow(WorkflowInstance workflow, CancellationToken _ = default) => _innerService.PersistWorkflow(workflow);

        public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
        {
            await PersistWorkflow(workflow, cancellationToken);

            foreach(var subscription in subscriptions)
            {
                await CreateEventSubscription(subscription, cancellationToken);
            }
        }

        public Task TerminateSubscription(string eventSubscriptionId, CancellationToken _ = default) => _innerService.TerminateSubscription(eventSubscriptionId);
        public Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken _ = default) => _innerService.GetSubscription(eventSubscriptionId);

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default) => _innerService.GetFirstOpenSubscription(eventName, eventKey, asOf);

        public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken _ = default) => _innerService.SetSubscriptionToken(eventSubscriptionId, token, workerId, expiry);

        public Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken _ = default) => _innerService.ClearSubscriptionToken(eventSubscriptionId, token);

        public Task ScheduleCommand(ScheduledCommand command)
        {
            throw new NotImplementedException();
        }

        public Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
