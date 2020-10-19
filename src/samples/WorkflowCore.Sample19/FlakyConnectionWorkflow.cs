using WorkflowCore.Interface;
using WorkflowCore.Sample19.Steps;

namespace WorkflowCore.Sample19
{
    public class FlakyConnectionWorkflow : IWorkflow<FlakyConnectionParams>
    {
        public string Id => "flaky-sample";

        public int Version => 1;

        public void Build(IWorkflowBuilder<FlakyConnectionParams> builder)
        {
            builder
                .StartWith<LogMessage>()
                .Input(x => x.Message, _ => "Starting workflow")

                .Then<FlakyConnection>()
                .Input(x => x.SucceedAfterAttempts, _ => 3)

                .Then<LogMessage>()
                .Input(x => x.Message, _ => "Finishing workflow");
        }
    }
}
