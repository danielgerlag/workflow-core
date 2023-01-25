using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Azure.Services
{
    public class AzureStorageQueueProvider : IQueueProvider
    {
        private readonly Dictionary<(QueueType, QueuePriority), CloudQueue> _queues = new Dictionary<(QueueType, QueuePriority), CloudQueue>();

        public bool IsDequeueBlocking => false;

        public AzureStorageQueueProvider(string connectionString, ILoggerFactory logFactory)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudQueueClient();

            _queues[(QueueType.Workflow, QueuePriority.Normal)] = client.GetQueueReference("workflowcore-workflows");
            _queues[(QueueType.Workflow, QueuePriority.High)] = client.GetQueueReference("workflowcore-workflowshigh");
            _queues[(QueueType.Event, QueuePriority.Normal)] = client.GetQueueReference("workflowcore-events");
            _queues[(QueueType.Index, QueuePriority.Normal)] = client.GetQueueReference("workflowcore-index");
        }

        private IEnumerable<CloudQueue> GetQueuesSortedByPriority(QueueType queue)
        {
            return _queues
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        public async Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            var msg = new CloudQueueMessage(id);
            await _queues[(queue, priority)].AddMessageAsync(msg);
        }

        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        private async Task<string> DequeueWork(CloudQueue cloudQueue, CancellationToken cancellationToken)
        {
            if (cloudQueue == null)
                return null;
            
            var msg = await cloudQueue.GetMessageAsync();

            if (msg == null)
                return null;

            await cloudQueue.DeleteMessageAsync(msg);
            return msg.AsString;
        }

        public async Task<string> DequeueWork(CloudQueue cloudQueue)
        {
            var msg = await cloudQueue.GetMessageAsync();

            if (msg == null)
                return null;

            await cloudQueue.DeleteMessageAsync(msg);
            return msg.AsString;
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

        public async Task Start()
        {
            foreach (var queue in _queues.Values)
            {
                await queue.CreateIfNotExistsAsync();
            }
        }

        public Task Stop() => Task.CompletedTask;

        public void Dispose()
        {
        }


    }
}
