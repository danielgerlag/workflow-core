using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ICancellationProcessor
    {
        void ProcessCancellations(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult executionResult);
    }
}
