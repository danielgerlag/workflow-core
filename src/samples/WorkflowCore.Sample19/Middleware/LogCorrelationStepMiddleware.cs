using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19.Middleware
{
    /// <summary>
    /// Loosely based off this article:
    /// https://www.frakkingsweet.com/net-core-log-correlation-easy-access-to-headers/
    /// </summary>
    public class AddMetadataToLogsMiddleware: IWorkflowStepMiddleware
    {
        private readonly ILogger<AddMetadataToLogsMiddleware> _log;

        public AddMetadataToLogsMiddleware(ILogger<AddMetadataToLogsMiddleware> log)
        {
            _log = log;
        }

        public async Task<ExecutionResult> HandleAsync(
            IStepExecutionContext context,
            IStepBody body,
            WorkflowStepDelegate next)
        {
            var workflowId = context.Workflow.Id;
            var stepId = context.Step.Id;

            using (_log.BeginScope("WorkflowId => {@WorkflowId}", workflowId))
            using (_log.BeginScope("StepId => {@StepId}", stepId))
            {
                return await next();
            }
        }
    }
}
