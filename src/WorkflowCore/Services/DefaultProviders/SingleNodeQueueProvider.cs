using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    /// <summary>
    /// Single node in-memory implementation of IQueueProvider
    /// </summary>
    public class SingleNodeQueueProvider : IQueueProvider
    {

        private readonly Dictionary<(QueueType, QueuePriority), BlockingCollection<string>> _queues = new Dictionary<(QueueType, QueuePriority), BlockingCollection<string>>
        {
            [(QueueType.Workflow, QueuePriority.Normal)] = new BlockingCollection<string>(),
            [(QueueType.Workflow, QueuePriority.High)] = new BlockingCollection<string>(),
            [(QueueType.Event, QueuePriority.Normal)] = new BlockingCollection<string>(),
            [(QueueType.Index, QueuePriority.Normal)] = new BlockingCollection<string>()
        };

        private IEnumerable<BlockingCollection<string>> GetQueuesSortedByPriority(QueueType queue)
        {
            return _queues
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        public bool IsDequeueBlocking => true;

        public Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            _queues[(queue, priority)].Add(id);
            return Task.CompletedTask;
        }

        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        private Task<string> DequeueWork(BlockingCollection<string> queue, CancellationToken cancellationToken)
        {
            if (queue.TryTake(out string id, 100, cancellationToken))
                return Task.FromResult(id);

            return Task.FromResult<string>(null);
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            foreach (var q in GetQueuesSortedByPriority(queue))
            {
                var result = await DequeueWork(q, cancellationToken);
                if (result != null)
                    return result;
            }

            return null;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
