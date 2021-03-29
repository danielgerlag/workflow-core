using System;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowErrorHandler
    {
        WorkflowErrorHandling Type { get; }
        void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue);
    }
}
