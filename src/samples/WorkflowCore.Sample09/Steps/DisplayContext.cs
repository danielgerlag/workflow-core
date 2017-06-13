using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample09
{
    public class DisplayContext : StepBody
    {        

        public object Item { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine($"Working on item {Item}");
            return ExecutionResult.Next();
        }
    }
}
