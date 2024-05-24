using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Azure.Services
{
    public class AzureStorageQueueProvider : IQueueProvider
    {
        private readonly ILogger _logger;
        
        private readonly Dictionary<QueueType, QueueClient> _queues = new Dictionary<QueueType, QueueClient>();

        public bool IsDequeueBlocking => false;

        public AzureStorageQueueProvider(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureStorageQueueProvider>();
            var client = new QueueServiceClient(connectionString);

            _queues[QueueType.Workflow] = client.GetQueueClient("workflowcore-workflows");
            _queues[QueueType.Event] = client.GetQueueClient("workflowcore-events");
            _queues[QueueType.Index] = client.GetQueueClient("workflowcore-index");
        }

        public AzureStorageQueueProvider(Uri queueEndpoint, TokenCredential tokenCredential, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureStorageQueueProvider>();
            var client = new QueueServiceClient(queueEndpoint, tokenCredential);

            _queues[QueueType.Workflow] = client.GetQueueClient("workflowcore-workflows");
            _queues[QueueType.Event] = client.GetQueueClient("workflowcore-events");
            _queues[QueueType.Index] = client.GetQueueClient("workflowcore-index");
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            await _queues[queue].SendMessageAsync(id);
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            var msg = await _queues[queue].ReceiveMessageAsync();

            if (msg == null || msg.Value == null)
                return null;

            await _queues[queue].DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt);
            return msg.Value.Body.ToString();
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
