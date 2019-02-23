using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    /// <remarks>
    /// The implemention of this interface will be responsible for
    /// providing a (distributed) queueing mechanism to manage in flight workflows    
    /// </remarks>
    public interface IQueueProvider : IDisposable
    {

        /// <summary>
        /// Enqueues work to be processed by a host in the cluster
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        Task QueueWork(string id, QueueType queue);

        /// <summary>
        /// Fetches the next work item from the front of the process queue.
        /// If the queue is empty, NULL is returned
        /// </summary>
        /// <returns></returns>
        Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken);

        bool IsDequeueBlocking { get; }

        Task Start();

        Task Stop();
    }

    public enum QueueType { Workflow = 0, Event = 1, Index = 2 }
}
