using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample06.Steps
{
    public class TaskC : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Doing Task C");
            return ExecutionResult.Next();
        }
    }
}
