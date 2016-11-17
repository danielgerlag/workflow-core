using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <remarks>
    /// The implemention of this interface will be responsible for
    /// providing a (distributed) queueing mechanism to manage in flight workflows
    /// and a distributed locking mechanism
    /// </remarks>
    public interface IConcurrencyProvider
    {
        void StartupNode();

        void ShutdownNode();

        Task EnqueueForProcessing(string Id);

        Task<string> DequeueForProcessing();

        Task<bool> AcquireLock(string Id);

        Task ReleaseLock(string Id);

        Task EnqueueForPublishing(EventPublication item);

        Task EnqueueForDeferredPublishing(EventPublication item);

        Task<EventPublication> DequeueForPublishing();

    }
}
