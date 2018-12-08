using System;
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
        private string _workflowQueue;
        private string _eventQueue;

        public bool IsDequeueBlocking => true;

        public SQSQueueProvider(AWSCredentials credentials, AmazonSQSConfig config, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<SQSQueueProvider>();
            _client = new AmazonSQSClient(credentials, config);
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            var queueUrl = string.Empty;
            switch (queue)
            {
                case QueueType.Workflow:
                    queueUrl = _workflowQueue;
                    break;
                case QueueType.Event:
                    queueUrl = _eventQueue;
                    break;
            }

            await _client.SendMessageAsync(new SendMessageRequest(queueUrl, id));
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            var queueUrl = string.Empty;
            switch (queue)
            {
                case QueueType.Workflow:
                    queueUrl = _workflowQueue;
                    break;
                case QueueType.Event:
                    queueUrl = _eventQueue;
                    break;
            }

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
            var workflowQueue = await _client.CreateQueueAsync(new CreateQueueRequest("workflowcore-workflows"));
            var eventQueue = await _client.CreateQueueAsync(new CreateQueueRequest("workflowcore-events"));

            _workflowQueue = workflowQueue.QueueUrl;
            _eventQueue = eventQueue.QueueUrl;
        }

        public Task Stop() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
