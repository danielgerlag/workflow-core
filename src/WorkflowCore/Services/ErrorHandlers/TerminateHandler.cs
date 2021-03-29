using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class TerminateHandler : IWorkflowErrorHandler
    {
        private readonly ILifeCycleEventPublisher _eventPublisher;
        private readonly IDateTimeProvider _dateTimeProvider;
        public WorkflowErrorHandling Type => WorkflowErrorHandling.Terminate;

        public TerminateHandler(ILifeCycleEventPublisher eventPublisher, IDateTimeProvider dateTimeProvider)
        {
            _eventPublisher = eventPublisher;
            _dateTimeProvider = dateTimeProvider;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            workflow.Status = WorkflowStatus.Terminated;
            workflow.CompleteTime = _dateTimeProvider.UtcNow;

            _eventPublisher.PublishNotification(new WorkflowTerminated
            {
                EventTimeUtc = _dateTimeProvider.UtcNow,
                Reference = workflow.Reference,
                WorkflowInstanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version
            });
        }
    }
}
