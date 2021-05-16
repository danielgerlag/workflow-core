using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample03.Steps
{
    public class GoodbyeWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Goodbye world");
            return ExecutionResult.Next();
        }
    }
}
