using System;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Primitives
{
    public class EscalateStep : WorkflowStep<Escalate>
    {
        
        public override void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            base.AfterWorkflowIteration(executorResult, defintion, workflow, executionPointer);
            var taskStep = workflow.ExecutionPointers.FindById(executionPointer.PredecessorId);

            if (taskStep.EventPublished)
            {
                executionPointer.EndTime = DateTime.Now.ToUniversalTime();
                executionPointer.Active = false;
            }
        }
    }
}
