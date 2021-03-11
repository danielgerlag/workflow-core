using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample09s
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
