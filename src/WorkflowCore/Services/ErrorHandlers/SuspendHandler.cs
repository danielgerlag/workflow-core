using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class SuspendHandler : IWorkflowErrorHandler
    {
        private readonly ILifeCycleEventHub _eventHub;
        private readonly IDateTimeProvider _datetimeProvider;
        public WorkflowErrorHandling Type => WorkflowErrorHandling.Suspend;

        public SuspendHandler(ILifeCycleEventHub eventHub, IDateTimeProvider datetimeProvider)
        {
            _eventHub = eventHub;
            _datetimeProvider = datetimeProvider;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            workflow.Status = WorkflowStatus.Suspended;
            _eventHub.PublishNotification(new WorkflowSuspended()
            {
                EventTimeUtc = _datetimeProvider.Now,
                Reference = workflow.Reference,
                WorkflowInsanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version
            });
        }
    }
}
