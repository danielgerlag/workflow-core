using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class Delay : StepBody
    {
        public TimeSpan Period { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData != null)
            {
                return ExecutionResult.Next();
            }
            
            return ExecutionResult.Sleep(Period, true);
        }
    }
}
