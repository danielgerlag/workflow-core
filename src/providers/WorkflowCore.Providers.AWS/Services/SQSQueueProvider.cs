using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class SQSQueueProvider : IQueueProvider
    {
        private const int WaitTime = 5;
        private readonly ILogger _logger;
        private readonly IAmazonSQS _client;
        private readonly Dictionary<QueueType, string> _queues = new Dictionary<QueueType, string>();
        private readonly string _queuesPrefix;

        public bool IsDequeueBlocking => true;

        public SQSQueueProvider(AmazonSQSClient sqsClient, ILoggerFactory logFactory, string queuesPrefix)
        {
            _logger = logFactory.CreateLogger<SQSQueueProvider>();
            _client = sqsClient;
            _queuesPrefix = queuesPrefix;
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            var queueUrl = _queues[queue];

            await _client.SendMessageAsync(new SendMessageRequest(queueUrl, id));
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            var queueUrl = _queues[queue];

            var result = await _client.ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl)
            {
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = WaitTime
            });

            if (result.Messages.Count == 0)
                return null;

            var msg = result.Messages.First();

            await _client.DeleteMessageAsync(new DeleteMessageRequest(queueUrl, msg.ReceiptHandle));
            return msg.Body;
        }

        public async Task Start()
        {
            var workflowQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-workflows"));
            var eventQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-events"));
            var indexQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-index"));

            _queues[QueueType.Workflow] = workflowQueue.QueueUrl;
            _queues[QueueType.Event] = eventQueue.QueueUrl;
            _queues[QueueType.Index] = indexQueue.QueueUrl;
        }

        public Task Stop() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
