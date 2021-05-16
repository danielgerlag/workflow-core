using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample09
{    
    public class SayGoodbye : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Goodbye");
            return ExecutionResult.Next();
        }
    }
}
