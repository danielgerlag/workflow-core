using System;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IExecutionResultProcessor
    {
        void HandleStepException(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception);
        void ProcessExecutionResult(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, ExecutionResult result, WorkflowExecutorResult workflowResult);
    }
}