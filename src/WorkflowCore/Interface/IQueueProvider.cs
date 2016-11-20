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
    /// </remarks>
    public interface IQueueProvider : IDisposable
    {

        /// <summary>
        /// Queues the workflow to be processed by a host in the cluster
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        Task QueueForProcessing(string Id);

        /// <summary>
        /// Fetches the next workflow instance from the front of the process queue.
        /// If the queue is empty, NULL is returned
        /// </summary>
        /// <returns></returns>
        Task<string> DequeueForProcessing();        

        Task QueueForPublishing(EventPublication item);

        /// <summary>
        /// Fetches the next published event from the front of the queue.
        /// If the queue is empty, NULL is returned
        /// </summary>
        /// <returns></returns>
        Task<EventPublication> DequeueForPublishing();

        void Start();
        void Stop();

    }
}
