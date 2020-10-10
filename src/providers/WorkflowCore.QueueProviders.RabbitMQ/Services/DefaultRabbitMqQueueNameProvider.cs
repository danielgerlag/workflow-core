using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.RabbitMQ.Interfaces;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
    public class DefaultRabbitMqQueueNameProvider : IRabbitMqQueueNameProvider
    {
        public string GetQueueName(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return "wfc.workflow_queue";
                case QueueType.Event:
                    return "wfc.event_queue";
                case QueueType.Index:
                    return "wfc.index_queue";
                default:
                    return null;
            }
        }
    }
}