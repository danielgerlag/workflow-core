using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary>
    /// Single node in-memory implementation of IQueueProvider
    /// </summary>
    public class SingleNodeQueueProvider : IQueueProvider
    {
                
        private readonly BlockingCollection<string> _runQueue = new BlockingCollection<string>();
        private readonly BlockingCollection<string> _eventQueue = new BlockingCollection<string>();

        public bool IsDequeueBlocking => true;

        public async Task QueueWork(string id, QueueType queue)
        {
            SelectQueue(queue).Add(id);
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (SelectQueue(queue).TryTake(out string id, 100, cancellationToken))
                return id;

            return null;
        }

        public async Task Start()
        {
        }

        public async Task Stop()
        {
        }

        public void Dispose()
        {            
        }

        private BlockingCollection<string> SelectQueue(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return _runQueue;                    
                case QueueType.Event:
                    return _eventQueue;
            }
            return null;
        }
        
    }

    #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
