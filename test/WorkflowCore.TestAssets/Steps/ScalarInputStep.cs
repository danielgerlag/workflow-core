using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestAssets.Steps
{
    public class ScalarInputStep : StepBody
    {
        public string MessageId { get; set; }

        public string Status { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Next();
        }
    }
}
