using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample09s
{    
    public class DoSomething : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Doing something...");
            return ExecutionResult.Next();
        }
    }
}
