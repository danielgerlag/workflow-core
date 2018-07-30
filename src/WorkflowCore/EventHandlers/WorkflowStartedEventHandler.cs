using System.Threading.Tasks;
using WorkflowCore.EventBus.Abstractions;
using WorkflowCore.Events;
using WorkflowCore.Interface;

namespace WorkflowCore.EventHandlers
{
    public class WorkflowStartedEventHandler : IIntegrationEventHandler<WorkflowStartedEvent>
    {
        private readonly IWorkflowWaitTaskStore _workflowWaitTaskStore;

        public WorkflowStartedEventHandler(IWorkflowWaitTaskStore workflowWaitTaskStore)
        {
            _workflowWaitTaskStore = workflowWaitTaskStore;
        }

        public Task Handle(WorkflowStartedEvent @event)
        {
            _workflowWaitTaskStore.AddTask(@event.WorkflowInstanceId);
            return Task.CompletedTask;
        }
    }
}