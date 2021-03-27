using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestAssets.Steps
{
    public class Counter : StepBody
    {
        public int Value { get; set; }
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Value++;
            return ExecutionResult.Next();
        }
    }
}
