using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestAssets.Steps
{

    public class Counter : StepBody
    {
        public int Value { get; set; }
        public NestedCounter A { get; set; } = new NestedCounter();

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            A.Value++;
            Value++;
            return ExecutionResult.Next();
        }
    }

    public class NestedCounter
    {
        public int Value { get; set; }
    }
}
