using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{

    public interface ISingletonMemoryProvider : IPersistenceProvider
    {
    }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary>
    /// In-memory implementation of IPersistenceProvider for demo and testing purposes
    /// </summary>
    public class MemoryPersistenceProvider : ISingletonMemoryProvider
    {
        private readonly List<WorkflowInstance> _instances = new List<WorkflowInstance>();
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private readonly List<Event> _events = new List<Event>();
        private readonly List<ExecutionError> _errors = new List<ExecutionError>();

        public bool SupportsScheduledCommands => false;

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken _ = default)
        {
            lock (_instances)
            {
                workflow.Id = Guid.NewGuid().ToString();
                _instances.Add(workflow);
                return workflow.Id;
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken _ = default)
        {
            lock (_instances)
            {
                var existing = _instances.First(x => x.Id == workflow.Id);
                _instances.Remove(existing);
                _instances.Add(workflow);
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
        {
            lock (_instances)
            {
                var existing = _instances.First(x => x.Id == workflow.Id);
                _instances.Remove(existing);
                _instances.Add(workflow);

                lock (_subscriptions)
                {
                    foreach (var subscription in subscriptions)
                    {
                        subscription.Id = Guid.NewGuid().ToString();
                        _subscriptions.Add(subscription);
                    }
                }
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken _ = default)
        {
            lock (_instances)
            {
                var now = asAt.ToUniversalTime().Ticks;
                return _instances.Where(x => x.NextExecution.HasValue && x.NextExecution <= now).Select(x => x.Id).ToList();
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id, CancellationToken _ = default)
        {
            lock (_instances)
            {
                return _instances.First(x => x.Id == Id);
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken _ = default)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            lock (_instances)
            {
                return _instances.Where(x => ids.Contains(x.Id));
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            lock (_instances)
            {
                var result = _instances.AsQueryable();

                if (status.HasValue)
                {
                    result = result.Where(x => x.Status == status.Value);
                }

                if (!String.IsNullOrEmpty(type))
                {
                    result = result.Where(x => x.WorkflowDefinitionId == type);
                }

                if (createdFrom.HasValue)
                {
                    result = result.Where(x => x.CreateTime >= createdFrom.Value);
                }

                if (createdTo.HasValue)
                {
                    result = result.Where(x => x.CreateTime <= createdTo.Value);
                }

                return result.Skip(skip).Take(take).ToList();
            }
        }


        public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                subscription.Id = Guid.NewGuid().ToString();
                _subscriptions.Add(subscription);
                return subscription.Id;
            }
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                return _subscriptions
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            }
        }

        public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
                _subscriptions.Remove(sub);
            }
        }

        public Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
                return Task.FromResult(sub);
            }
        }

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                var result =  _subscriptions
                    .FirstOrDefault(x => x.ExternalToken == null &&  x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
                return Task.FromResult(result);
            }
        }

        public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
                sub.ExternalToken = token;
                sub.ExternalWorkerId = workerId;
                sub.ExternalTokenExpiry = expiry;
                
                return Task.FromResult(true);
            }
        }

        public Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken _ = default)
        {
            lock (_subscriptions)
            {
                var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
                if (sub.ExternalToken != token)
                    throw new InvalidOperationException();
                sub.ExternalToken = null;
                sub.ExternalWorkerId = null;
                sub.ExternalTokenExpiry = null;

                return Task.CompletedTask;
            }
        }

        public void EnsureStoreExists()
        {
        }

        public async Task<string> CreateEvent(Event newEvent, CancellationToken _ = default)
        {
            lock (_events)
            {
                newEvent.Id = Guid.NewGuid().ToString();
                _events.Add(newEvent);
                return newEvent.Id;
            }
        }

        public async Task MarkEventProcessed(string id, CancellationToken _ = default)
        {
            lock (_events)
            {
                var evt = _events.FirstOrDefault(x => x.Id == id);
                if (evt != null)
                    evt.IsProcessed = true;
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken _ = default)
        {
            lock (_events)
            {
                return _events
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= asAt.ToUniversalTime())
                    .Select(x => x.Id)
                    .ToList();
            }
        }

        public async Task<Event> GetEvent(string id, CancellationToken _ = default)
        {
            lock (_events)
            {
                return _events.FirstOrDefault(x => x.Id == id);
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken _ = default)
        {
            lock (_events)
            {
                return _events
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .Select(x => x.Id)
                    .ToList();
            }
        }

        public async Task MarkEventUnprocessed(string id, CancellationToken _ = default)
        {
            lock (_events)
            {
                var evt = _events.FirstOrDefault(x => x.Id == id);
                if (evt != null)
                {
                    evt.IsProcessed = false;
                }
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken _ = default)
        {
            lock (errors)
            {
                _errors.AddRange(errors);
            }
        }

        public Task ScheduleCommand(ScheduledCommand command)
        {
            throw new NotImplementedException();
        }

        public Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
