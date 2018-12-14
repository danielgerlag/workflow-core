using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class TerminateHandler : IWorkflowErrorHandler
    {
        private readonly ILifeCycleEventHub _eventHub;
        private readonly IDateTimeProvider _datetimeProvider;
        public WorkflowErrorHandling Type => WorkflowErrorHandling.Terminate;

        public TerminateHandler(ILifeCycleEventHub eventHub, IDateTimeProvider datetimeProvider)
        {
            _eventHub = eventHub;
            _datetimeProvider = datetimeProvider;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            workflow.Status = WorkflowStatus.Terminated;
            _eventHub.PublishNotification(new WorkflowTerminated()
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
