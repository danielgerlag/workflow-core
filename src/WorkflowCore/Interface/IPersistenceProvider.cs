using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <remarks>
    /// The implemention of this interface will be responsible for
    /// persisiting running workflow instances to a durable store
    /// It also provides a (distributed) queueing mechanism to manage in flight workflows
    /// and a distributed locking mechanism (todo)
    /// </remarks>
    public interface IPersistenceProvider
    {
        Task<string> CreateNewWorkflow(WorkflowInstance workflow);

        Task PersistWorkflow(WorkflowInstance workflow);

        Task<IEnumerable<string>> GetRunnableInstances();

        Task<WorkflowInstance> GetWorkflowInstance(string Id);

        Task<string> CreateEventSubscription(EventSubscription subscription);

        Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey);

        Task TerminateSubscription(string eventSubscriptionId);

        Task CreateUnpublishedEvent(EventPublication publication);

        Task<IEnumerable<EventPublication>> GetUnpublishedEvents();

        Task RemoveUnpublishedEvent(Guid id);

        void EnsureStoreExists();

    }
}
