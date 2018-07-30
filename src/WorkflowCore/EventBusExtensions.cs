using WorkflowCore.EventBus.Abstractions;
using WorkflowCore.Events;

namespace WorkflowCore
{
    public static class EventBusExtensions
    {
        public static void WorkflowStarted(this IEventBus eventBus, string workflowInstanceId)
        {
            var @event = new WorkflowStartedEvent
            {
                WorkflowInstanceId = workflowInstanceId
            };
            eventBus.HandleEventAndPublish(@event);
        }

        public static void WorkflowComplete(this IEventBus eventBus, string workflowInstanceId)
        {
            var @event = new WorkflowCompleteEvent
            {
                WorkflowInstanceId = workflowInstanceId
            };
            eventBus.HandleEventAndPublish(@event);
        }
    }
}
