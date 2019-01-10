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
                    _logger.LogError(default(EventId), ex.Message, ex);
                }
                if (cancel)
                {
                    var toCancel = new Stack<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.StepId == step.Id && x.Status != PointerStatus.Complete && x.Status != PointerStatus.Cancelled));

                    while (toCancel.Count > 0)
                    {
                        var ptr = toCancel.Pop();

                        ptr.EndTime = DateTime.Now.ToUniversalTime();
                        ptr.Active = false;
                        ptr.Status = PointerStatus.Cancelled;

                        if ((ptr.StepId == step.Id) && (step.ProceedOnCancel))
                        {
                            _executionResultProcessor.ProcessExecutionResult(workflow, workflowDef, ptr, step, ExecutionResult.Next(), executionResult);
                        }

                        foreach (var childId in ptr.Children)
                        {
                            var child = workflow.ExecutionPointers.FindById(childId);
                            if (child != null)
                                toCancel.Push(child);
                        }
                    }
                }
            }
        }
    }
}
