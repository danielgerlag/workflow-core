using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestSample01.Workflow
{
    public class AddNumbers : StepBody
    {
        public int Input1 { get; set; }
        public int Input2 { get; set; }
        public int Output { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Output = (Input1 + Input2);
            return ExecutionResult.Next();
        }
    }
}
