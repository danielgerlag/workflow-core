using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.RabbitMQ.Interfaces;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
    public class DefaultRabbitMqQueueNameProvider : IRabbitMqQueueNameProvider
    {
        private readonly Dictionary<(QueueType, QueuePriority), string> _queues = new Dictionary<(QueueType, QueuePriority), string>
        {
            [(QueueType.Workflow, QueuePriority.Normal)] = "wfc.workflow_queue",
            [(QueueType.Workflow, QueuePriority.High)] = "wfc.workflowhigh_queue",
            [(QueueType.Event, QueuePriority.Normal)] = "wfc.event_queue",
            [(QueueType.Index, QueuePriority.Normal)] = "wfc.index_queue"
        };

        public IDictionary<(QueueType, QueuePriority), string> GetAll() => _queues;

        public string GetQueueName(QueueType queue, QueuePriority priority)
        {
            return _queues[(queue, priority)];
        }

        public string GetQueueName(QueueType queue)
        {
            return GetQueueName(queue, QueuePriority.Normal);
        }
    }
}