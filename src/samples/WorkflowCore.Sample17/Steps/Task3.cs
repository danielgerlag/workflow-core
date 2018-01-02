using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample17.Steps
{
    public class Task3 : StepBody
    { 
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Doing Task 3");
            return ExecutionResult.Next();
        }
    }
}
