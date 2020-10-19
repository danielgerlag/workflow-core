using System;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19.Steps
{
    public class FlakyConnection : StepBodyAsync
    {
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(1);
        private int _currentCallCount = 0;

        public int? SucceedAfterAttempts { get; set; } = 3;

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (SucceedAfterAttempts.HasValue && _currentCallCount >= SucceedAfterAttempts.Value)
            {
                return ExecutionResult.Next();
            }

            _currentCallCount++;
            await Task.Delay(Delay);
            throw new TimeoutException("A call has timed out");
        }
    }
}
