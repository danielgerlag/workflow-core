using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample20.Steps
{
    public class ProcessBatch : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (BatchData)context.Workflow.Data;
            data.ProcessedCount = data.Items.Count;

            Console.WriteLine($"Workflow {context.Workflow.Id} processing {data.ProcessedCount} item(s): [{string.Join(", ", data.Items)}]");
            return ExecutionResult.Next();
        }
    }
}
