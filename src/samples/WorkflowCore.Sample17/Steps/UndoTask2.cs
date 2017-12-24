using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample17.Steps
{
    public class UndoTask2 : StepBody
    { 
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Undoing Task 2");
            return ExecutionResult.Next();
        }
    }
}
