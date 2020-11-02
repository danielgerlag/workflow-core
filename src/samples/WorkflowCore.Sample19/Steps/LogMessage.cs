using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19.Steps
{
    public class LogMessage : StepBodyAsync
    {
        private readonly ILogger<LogMessage> _log;

        public LogMessage(ILogger<LogMessage> log)
        {
            _log = log;
        }

        public string Message { get; set; }

        public override Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (Message != null)
            {
                _log.LogInformation(Message);
            }

            return Task.FromResult(ExecutionResult.Next());
        }
    }
}
