using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class ActionStepBody : StepBody
    {
        public Action<IStepExecutionContext> Body { get; set; }
        
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Body(context);
            return ExecutionResult.Next();
        }
    }
}
