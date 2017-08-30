using System;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class EndStep : WorkflowStep
    {
        public override Type BodyType => null;

        public override ExecutionPipelineDirective InitForExecution(
            WorkflowExecutorResult executorResult, 
            WorkflowDefinition defintion, 
            WorkflowInstance workflow, 
            ExecutionPointer executionPointer)
        {
            return ExecutionPipelineDirective.EndWorkflow;
        }
    }
}
