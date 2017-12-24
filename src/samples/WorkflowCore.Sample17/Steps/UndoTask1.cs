using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample17.Steps
{
    public class UndoTask1 : StepBody
    { 
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Undoing Task 1");
            return ExecutionResult.Next();
        }
    }
}
