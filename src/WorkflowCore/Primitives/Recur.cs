using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class Recur : ContainerStepBody
    {
        public TimeSpan Interval { get; set; }

        public bool StopCondition { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (StopCondition)
            {
                return ExecutionResult.Next();
            }

            return new ExecutionResult
            {
                Proceed = false,
                BranchValues = new List<object> { null },
                SleepFor = Interval
            };
        }
    }
}
