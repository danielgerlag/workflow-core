using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.EventBus.Abstractions;
using WorkflowCore.Events;
using WorkflowCore.Interface;

namespace WorkflowCore.EventHandlers
{
    public class WorkflowCompleteEventHandler : IIntegrationEventHandler<WorkflowCompleteEvent>
    {
        private readonly IWorkflowWaitTaskStore _workflowWaitTaskStore;

        public WorkflowCompleteEventHandler(IWorkflowWaitTaskStore workflowWaitTaskStore)
        {
            _workflowWaitTaskStore = workflowWaitTaskStore;
        }

        public Task Handle(WorkflowCompleteEvent @event)
        {
            _workflowWaitTaskStore.RemoveTask(@event.WorkflowInstanceId);
            return Task.CompletedTask;
        }
    }
}