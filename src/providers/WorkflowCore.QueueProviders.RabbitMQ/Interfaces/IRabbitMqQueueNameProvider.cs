using System.Collections.Generic;
using WorkflowCore.Interface;

namespace WorkflowCore.QueueProviders.RabbitMQ.Interfaces
{
    public interface IRabbitMqQueueNameProvider
    {
        IDictionary<(QueueType, QueuePriority), string> GetAll();
        string GetQueueName(QueueType queue, QueuePriority priority);
        string GetQueueName(QueueType queue);
    }
}