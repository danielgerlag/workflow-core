using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Tests.YmalDefinition.Steps
{
    public class HelloWorld : StepBody
    {
        public int Value { get; set; } = 10;
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Value is " + Value);
            return ExecutionResult.Next();
        }
    }
}
