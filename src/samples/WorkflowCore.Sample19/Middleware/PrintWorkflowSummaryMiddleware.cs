using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19.Middleware
{
    public class PrintWorkflowSummaryMiddleware : IWorkflowMiddleware
    {
        private readonly ILogger<PrintWorkflowSummaryMiddleware> _log;

        public PrintWorkflowSummaryMiddleware(ILogger<PrintWorkflowSummaryMiddleware> log)
        {
            _log = log;
        }

        public WorkflowMiddlewarePhase Phase => WorkflowMiddlewarePhase.PostWorkflow;

        public Task HandleAsync(WorkflowInstance workflow, WorkflowDelegate next)
        {
            if (!workflow.CompleteTime.HasValue)
            {
                return next();
            }

            var duration = workflow.CompleteTime.Value - workflow.CreateTime;
            _log.LogInformation($@"Workflow {workflow.Description} completed in {duration:g}");

            foreach (var step in workflow.ExecutionPointers)
            {
                var stepName = step.StepName;
                var stepDuration = (step.EndTime - step.StartTime) ?? TimeSpan.Zero;
                _log.LogInformation($"  - Step {stepName} completed in {stepDuration:g}");
            }

            return next();
        }
    }
}
