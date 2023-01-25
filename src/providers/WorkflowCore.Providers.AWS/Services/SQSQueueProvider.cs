using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class SQSQueueProvider : IQueueProvider
    {
        private const int WaitTime = 5;
        private readonly IAmazonSQS _client;
        private readonly Dictionary<(QueueType, QueuePriority), string> _queues = new Dictionary<(QueueType, QueuePriority), string>();
        private readonly string _queuesPrefix;

        private IEnumerable<string> GetQueuesSortedByPriority(QueueType queue)
        {
            return _queues
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        public bool IsDequeueBlocking => true;

        public SQSQueueProvider(AWSCredentials credentials, AmazonSQSConfig config, ILoggerFactory logFactory, string queuesPrefix)
        {
            _client = new AmazonSQSClient(credentials, config);
            _queuesPrefix = queuesPrefix;
        }

        public async Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            var queueUrl = _queues[(queue, priority)];

            await _client.SendMessageAsync(new SendMessageRequest(queueUrl, id));
        }

        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        private async Task<string> DequeueWork(string queueUrl)
        {
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

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            foreach (var queueUrl in GetQueuesSortedByPriority(queue))
            {
                var result = await DequeueWork(queueUrl);
                if (result != null)
                    return result;
            }

            return null;
        }

        public async Task Start()
        {
            var workflowQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-workflows"));
            var workflowhighQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-workflowshigh"));
            var eventQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-events"));
            var indexQueue = await _client.CreateQueueAsync(new CreateQueueRequest($"{_queuesPrefix}-index"));

            _queues[(QueueType.Workflow, QueuePriority.Normal)] = workflowQueue.QueueUrl;
            _queues[(QueueType.Workflow, QueuePriority.High)] = workflowhighQueue.QueueUrl;
            _queues[(QueueType.Event, QueuePriority.Normal)] = eventQueue.QueueUrl;
            _queues[(QueueType.Index, QueuePriority.Normal)] = indexQueue.QueueUrl;
        }

        public Task Stop() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
