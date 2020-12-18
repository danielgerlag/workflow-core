using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        protected readonly IScopeProvider _scopeProvider;
        protected readonly IDateTimeProvider _datetimeProvider;
        protected readonly ILogger _logger;
        private readonly IExecutionResultProcessor _executionResultProcessor;
        private readonly ICancellationProcessor _cancellationProcessor;
        private readonly ILifeCycleEventPublisher _publisher;
        private readonly WorkflowOptions _options;

        private IWorkflowHost Host => _serviceProvider.GetService<IWorkflowHost>();

        public WorkflowExecutor(IWorkflowRegistry registry, IServiceProvider serviceProvider, IScopeProvider scopeProvider, IDateTimeProvider datetimeProvider, IExecutionResultProcessor executionResultProcessor, ILifeCycleEventPublisher publisher, ICancellationProcessor cancellationProcessor, WorkflowOptions options, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _scopeProvider = scopeProvider;
            _registry = registry;
            _datetimeProvider = datetimeProvider;
            _publisher = publisher;
            _cancellationProcessor = cancellationProcessor;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowExecutor>();
            _executionResultProcessor = executionResultProcessor;
        }

        public async Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            var wfResult = new WorkflowExecutorResult();

            var exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < _datetimeProvider.UtcNow)));
            var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
            if (def == null)
            {
                _logger.LogError("Workflow {0} version {1} is not registered", workflow.WorkflowDefinitionId, workflow.Version);
                return wfResult;
            }

            _cancellationProcessor.ProcessCancellations(workflow, def, wfResult);

            foreach (var pointer in exePointers)
            {
                if (!pointer.Active)
                    continue;

                var step = def.Steps.FindById(pointer.StepId);
                if (step == null)
                {
                    _logger.LogError("Unable to find step {0} in workflow definition", pointer.StepId);
                    pointer.SleepUntil = _datetimeProvider.UtcNow.Add(_options.ErrorRetryInterval);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = _datetimeProvider.UtcNow,
                        Message = $"Unable to find step {pointer.StepId} in workflow definition"
                    });
                    continue;
                }

                try
                {
                    if (!InitializeStep(workflow, step, wfResult, def, pointer))
                        continue;

                    await ExecuteStep(workflow, step, pointer, wfResult, def, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = _datetimeProvider.UtcNow,
                        Message = ex.Message
                    });

                    _executionResultProcessor.HandleStepException(workflow, def, pointer, step, ex);
                    Host.ReportStepError(workflow, step, ex);
                }
                _cancellationProcessor.ProcessCancellations(workflow, def, wfResult);
            }
            ProcessAfterExecutionIteration(workflow, def, wfResult);
            await DetermineNextExecutionTime(workflow, def);

            return wfResult;
        }

        private bool InitializeStep(WorkflowInstance workflow, WorkflowStep step, WorkflowExecutorResult wfResult, WorkflowDefinition def, ExecutionPointer pointer)
        {
            switch (step.InitForExecution(wfResult, def, workflow, pointer))
            {
                case ExecutionPipelineDirective.Defer:
                    return false;
                case ExecutionPipelineDirective.EndWorkflow:
                    workflow.Status = WorkflowStatus.Complete;
                    workflow.CompleteTime = _datetimeProvider.UtcNow;
                    return false;
            }

            if (pointer.Status != PointerStatus.Running)
            {
                pointer.Status = PointerStatus.Running;
                _publisher.PublishNotification(new StepStarted()
                {
                    EventTimeUtc = _datetimeProvider.UtcNow,
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
                pointer.StartTime = _datetimeProvider.UtcNow;
            }

            return true;
        }

        private async Task ExecuteStep(WorkflowInstance workflow, WorkflowStep step, ExecutionPointer pointer, WorkflowExecutorResult wfResult, WorkflowDefinition def, CancellationToken cancellationToken = default)
        {
            IStepExecutionContext context = new StepExecutionContext()
            {
                Workflow = workflow,
                Step = step,
                PersistenceData = pointer.PersistenceData,
                ExecutionPointer = pointer,
                Item = pointer.ContextItem,
                CancellationToken = cancellationToken
            };

            using (var scope = _scopeProvider.CreateScope(context))
            {
                _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                IStepBody body = step.ConstructBody(scope.ServiceProvider);
                var stepExecutor = scope.ServiceProvider.GetRequiredService<IStepExecutor>();

                if (body == null)
                {
                    _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                    pointer.SleepUntil = _datetimeProvider.UtcNow.Add(_options.ErrorRetryInterval);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = _datetimeProvider.UtcNow,
                        Message = $"Unable to construct step body {step.BodyType.ToString()}"
                    });
                    return;
                }

                foreach (var input in step.Inputs)
                    input.AssignInput(workflow.Data, body, context);

                switch (step.BeforeExecute(wfResult, context, pointer, body))
                {
                    case ExecutionPipelineDirective.Defer:
                        return;
                    case ExecutionPipelineDirective.EndWorkflow:
                        workflow.Status = WorkflowStatus.Complete;
                        workflow.CompleteTime = _datetimeProvider.UtcNow;
                        return;
                }

                var result = await stepExecutor.ExecuteStep(context, body);

                if (result.Proceed)
                {
                    foreach (var output in step.Outputs)
                        output.AssignOutput(workflow.Data, body, context);
                }

                _executionResultProcessor.ProcessExecutionResult(workflow, def, pointer, step, result, wfResult);
                step.AfterExecute(wfResult, context, result, pointer);
            }
        }

        private void ProcessAfterExecutionIteration(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult workflowResult)
        {
            var pointers = workflow.ExecutionPointers.Where(x => x.EndTime == null);

            foreach (var pointer in pointers)
            {
                var step = workflowDef.Steps.FindById(pointer.StepId);
                step?.AfterWorkflowIteration(workflowResult, workflowDef, workflow, pointer);
            }
        }

        private async Task DetermineNextExecutionTime(WorkflowInstance workflow, WorkflowDefinition def)
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

            if ((workflow.NextExecution != null) || (workflow.ExecutionPointers.Any(x => x.EndTime == null)))
                return;

            workflow.Status = WorkflowStatus.Complete;
            workflow.CompleteTime = _datetimeProvider.UtcNow;

            using (var scope = _serviceProvider.CreateScope())
            {
                var middlewareRunner = scope.ServiceProvider.GetRequiredService<IWorkflowMiddlewareRunner>();
                await middlewareRunner.RunPostMiddleware(workflow, def);
            }

            _publisher.PublishNotification(new WorkflowCompleted()
            {
                EventTimeUtc = _datetimeProvider.UtcNow,
                Reference = workflow.Reference,
                WorkflowInstanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version
            });
        }
    }
}
