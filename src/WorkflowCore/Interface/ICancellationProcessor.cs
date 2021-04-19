using System;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ICancellationProcessor
    {
        void ProcessCancellations(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult executionResult);
    }
}
