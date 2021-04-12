using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample18.Steps
{
    public class CustomMessage : StepBody
    {
        
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine(Message);
            return ExecutionResult.Next();
        }
    }
}
