using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample15.Steps
{
    public class HelloWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Hello world");
            return ExecutionResult.Next();
        }
    }
}
