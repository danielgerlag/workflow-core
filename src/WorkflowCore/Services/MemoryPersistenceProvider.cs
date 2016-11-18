using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <summary>
    /// In-memory implementation of IPersistenceProvider for demo and testing purposes
    /// </summary>
    public class MemoryPersistenceProvider : IPersistenceProvider
    {

        private static List<WorkflowInstance> _instances = new List<WorkflowInstance>();
        private static List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private static List<EventPublication> _unpublishedEvents = new List<EventPublication>();

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();
            _instances.Add(workflow);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);
        }

        public async Task<IEnumerable<string>> GetRunnableInstances()
        {
            var now = DateTime.Now.ToUniversalTime().Ticks;
            return _instances.Where(x => x.NextExecution.HasValue && x.NextExecution <= now).Select(x => x.Id);
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            return _instances.First(x => x.Id == Id);
        }

        
        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();
            _subscriptions.Add(subscription);
            return subscription.Id;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey)
        {
            return _subscriptions
                .Where(x => x.EventName == eventName && x.EventKey == eventKey);
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            _subscriptions.Remove(sub);
        }

        public void EnsureStoreExists()
        {            
        }

        public async Task CreateUnpublishedEvent(EventPublication publication)
        {            
            _unpublishedEvents.Add(publication);
        }

        public async Task RemoveUnpublishedEvent(Guid id)
        {
            var evt = _unpublishedEvents.FirstOrDefault(x => x.Id == id);
            if (evt != null)
                _unpublishedEvents.Remove(evt);
        }

        public async Task<IEnumerable<EventPublication>> GetUnpublishedEvents()
        {
            return _unpublishedEvents;
        }
    }
}
