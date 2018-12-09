using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly ILogger _logger;
        private readonly CloudQueue _workflowQueue;
        private readonly CloudQueue _eventQueue;

        public bool IsDequeueBlocking => false;

        public AzureStorageQueueProvider(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureStorageQueueProvider>();
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudQueueClient();

            _workflowQueue = client.GetQueueReference("workflowcore-workflows");
            _eventQueue = client.GetQueueReference("workflowcore-events");                        
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            var msg = new CloudQueueMessage(id);

            switch (queue)
            {
                case QueueType.Workflow:
                    await _workflowQueue.AddMessageAsync(msg);
                    break;
                case QueueType.Event:
                    await _eventQueue.AddMessageAsync(msg);
                    break;
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            CloudQueue cloudQueue = null;
            switch (queue)
            {
                case QueueType.Workflow:
                    cloudQueue = _workflowQueue;
                    break;
                case QueueType.Event:
                    cloudQueue = _eventQueue;
                    break;
            }

            if (cloudQueue == null)
                return null;
            
            var msg = await cloudQueue.GetMessageAsync();

            if (msg == null)
                return null;

            await cloudQueue.DeleteMessageAsync(msg);
            return msg.AsString;
        }

        public async Task Start()
        {
            await _workflowQueue.CreateIfNotExistsAsync();
            await _eventQueue.CreateIfNotExistsAsync();
        }

        public Task Stop() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
