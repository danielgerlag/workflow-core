using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class CancellationProcessor : ICancellationProcessor
    {
        protected readonly ILogger _logger;
        private readonly IExecutionResultProcessor _executionResultProcessor;

        public CancellationProcessor(IExecutionResultProcessor executionResultProcessor, ILoggerFactory logFactory)
        {
            _executionResultProcessor = executionResultProcessor;
            _logger = logFactory.CreateLogger<CancellationProcessor>();
        }

        public void ProcessCancellations(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult executionResult)
        {
            foreach (var step in workflowDef.Steps.Where(x => x.CancelCondition != null))
            {
                var func = step.CancelCondition.Compile();
                var cancel = false;
                try
                {
                    cancel = (bool)(func.DynamicInvoke(workflow.Data));
                }
                catch (Exception ex)
                {
                    _logger.LogError(default(EventId), ex, ex.Message);
                }
                if (cancel)
                {
                    var toCancel = workflow.ExecutionPointers.Where(x => x.StepId == step.Id && x.Status != PointerStatus.Complete && x.Status != PointerStatus.Cancelled).ToList();

                    foreach (var ptr in toCancel)
                    {
                        if (step.ProceedOnCancel)
                        {
                            _executionResultProcessor.ProcessExecutionResult(workflow, workflowDef, ptr, step, ExecutionResult.Next(), executionResult);
                        }

                        ptr.EndTime = DateTime.Now.ToUniversalTime();
                        ptr.Active = false;
                        ptr.Status = PointerStatus.Cancelled;

                        foreach (var descendent in workflow.ExecutionPointers.FindByScope(ptr.Id).Where(x => x.Status != PointerStatus.Complete && x.Status != PointerStatus.Cancelled))
                        {
                            descendent.EndTime = DateTime.Now.ToUniversalTime();
                            descendent.Active = false;
                            descendent.Status = PointerStatus.Cancelled;
                        }
                    }
                }
            }
        }
    }
}
