using System;
using System.Linq;
using WorkflowCore.Interface;

namespace WorkflowCore.QueueProviders.RabbitMQ.Interfaces
{
    public interface IRabbitMqQueueNameProvider
    {
        string GetQueueName(QueueType queue);
    }
}