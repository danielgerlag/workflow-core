using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class WorkflowExecutor : IWorkflowExecutor
    {
        protected readonly IWorkflowRegistry _registry;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IDateTimeProvider _datetimeProvider;
        protected readonly ILogger _logger;
        private readonly IExecutionResultProcessor _executionResultProcessor;
        private readonly ICancellationProcessor _cancellationProcessor;
        private readonly ILifeCycleEventPublisher _publisher;
        private readonly WorkflowOptions _options;

        private IWorkflowHost Host => _serviceProvider.GetService<IWorkflowHost>();

        public WorkflowExecutor(IWorkflowRegistry registry, IServiceProvider serviceProvider, IDateTimeProvider datetimeProvider, IExecutionResultProcessor executionResultProcessor, ILifeCycleEventPublisher publisher, ICancellationProcessor cancellationProcessor, WorkflowOptions options, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _registry = registry;
            _datetimeProvider = datetimeProvider;
            _publisher = publisher;
            _cancellationProcessor = cancellationProcessor;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowExecutor>();
            _executionResultProcessor = executionResultProcessor;
        }

        public async Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow)
        {
            var wfResult = new WorkflowExecutorResult();

            var exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < _datetimeProvider.Now.ToUniversalTime())));
            var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
            if (def == null)
            {
                _logger.LogError("Workflow {0} version {1} is not registered", workflow.WorkflowDefinitionId, workflow.Version);
                return wfResult;
            }

            foreach (var pointer in exePointers)
            {
                if (pointer.Status == PointerStatus.Cancelled)
                    continue;

                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step != null)
                {
                    try
                    {                        
                        switch (step.InitForExecution(wfResult, def, workflow, pointer))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
                                continue;
                        }

                        if (pointer.Status != PointerStatus.Running)
                        {
                            pointer.Status = PointerStatus.Running;
                            _publisher.PublishNotification(new StepStarted()
                            {
                                EventTimeUtc = _datetimeProvider.Now,
                                Reference = workflow.Reference,
                                ExecutionPointerId = pointer.Id,
                                StepId = step.Id,
                                WorkflowInstanceId = workflow.Id,
                                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                                Version = workflow.Version
                            });
                        }

                        if (!pointer.StartTime.HasValue)
                        {
                            pointer.StartTime = _datetimeProvider.Now.ToUniversalTime();
                        }

                        _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                        IStepBody body = step.ConstructBody(_serviceProvider);

                        if (body == null)
                        {
                            _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                            pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(_options.ErrorRetryInterval);
                            wfResult.Errors.Add(new ExecutionError()
                            {
                                WorkflowId = workflow.Id,
                                ExecutionPointerId = pointer.Id,
                                ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                                Message = String.Format("Unable to construct step body {0}", step.BodyType.ToString())
                            });
                            continue;
                        }

                        IStepExecutionContext context = new StepExecutionContext()
                        {
                            Workflow = workflow,
                            Step = step,
                            PersistenceData = pointer.PersistenceData,
                            ExecutionPointer = pointer,
                            Item = pointer.ContextItem
                        };

                        foreach (var input in step.Inputs)
                            input.AssignInput(workflow.Data, body, context);


                        switch (step.BeforeExecute(wfResult, context, pointer, body))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
                                continue;
                        }

                        var result = await body.RunAsync(context);

                        if (result.Proceed)
                        {
                            foreach (var output in step.Outputs)
                                output.AssignOutput(workflow.Data, body, context);
                        }

                        _executionResultProcessor.ProcessExecutionResult(workflow, def, pointer, step, result, wfResult);
                        step.AfterExecute(wfResult, context, result, pointer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                        wfResult.Errors.Add(new ExecutionError()
                        {
                            WorkflowId = workflow.Id,
                            ExecutionPointerId = pointer.Id,
                            ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                            Message = ex.Message
                        });
                        
                        _executionResultProcessor.HandleStepException(workflow, def, pointer, step, ex);
                        Host.ReportStepError(workflow, step, ex);
                    }
                    _cancellationProcessor.ProcessCancellations(workflow, def, wfResult);
                }
                else
                {
                    _logger.LogError("Unable to find step {0} in workflow definition", pointer.StepId);
                    pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(_options.ErrorRetryInterval);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                        Message = String.Format("Unable to find step {0} in workflow definition", pointer.StepId)
                    });
                }

            }
            ProcessAfterExecutionIteration(workflow, def, wfResult);
            DetermineNextExecutionTime(workflow);

            return wfResult;
        }
        
        private void ProcessAfterExecutionIteration(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult workflowResult)
        {
            var pointers = workflow.ExecutionPointers.Where(x => x.EndTime == null);

            foreach (var pointer in pointers)
            {
                var step = workflowDef.Steps.First(x => x.Id == pointer.StepId);
                step?.AfterWorkflowIteration(workflowResult, workflowDef, workflow, pointer);
            }
        }

        private void DetermineNextExecutionTime(WorkflowInstance workflow)
        {
            //TODO: move to own class
            workflow.NextExecution = null;

            if (workflow.Status == WorkflowStatus.Complete)
                return;

            foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count == 0))
            {
                if (!pointer.SleepUntil.HasValue)
                {
                    workflow.NextExecution = 0;
                    return;
                }

                var pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
                workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
            }

            if (workflow.NextExecution == null)
            {
                foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count > 0))
                {
                    if (!workflow.ExecutionPointers.FindByScope(pointer.Id).All(x => x.EndTime.HasValue)) 
                        continue;
                    
                    if (!pointer.SleepUntil.HasValue)
                    {
                        workflow.NextExecution = 0;
                        return;
                    }

                    var pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
                    workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
                }
            }

            if ((workflow.NextExecution != null) || (workflow.ExecutionPointers.Any(x => x.EndTime == null))) 
                return;
            
            workflow.Status = WorkflowStatus.Complete;
            workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
            _publisher.PublishNotification(new WorkflowCompleted()
            {
                EventTimeUtc = _datetimeProvider.Now,
                Reference = workflow.Reference,
                WorkflowInstanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version
            });
        }
        
    }
}
