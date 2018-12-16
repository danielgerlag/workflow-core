using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.FluentBuilders;

namespace WorkflowCore.Services
{
    public class WorkflowExecutor : IWorkflowExecutor
    {
        protected readonly IWorkflowRegistry _registry;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IDateTimeProvider _datetimeProvider;
        protected readonly ILogger _logger;
        private readonly IExecutionResultProcessor _executionResultProcessor;
        private readonly WorkflowOptions _options;

        private IWorkflowHost Host => _serviceProvider.GetService<IWorkflowHost>();

        public WorkflowExecutor(IWorkflowRegistry registry, IServiceProvider serviceProvider, IDateTimeProvider datetimeProvider, IExecutionResultProcessor executionResultProcessor, WorkflowOptions options, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _registry = registry;
            _datetimeProvider = datetimeProvider;
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
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step != null)
                {
                    try
                    {
                        pointer.Status = PointerStatus.Running;
                        switch (step.InitForExecution(wfResult, def, workflow, pointer))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
                                continue;
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

                        ProcessInputs(workflow, step, body, context);

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
                            ProcessOutputs(workflow, step, body, context);
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

                        _executionResultProcessor.HandleStepException(workflow, def, pointer, step);
                        Host.ReportStepError(workflow, step, ex);
                    }
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

        private void ProcessInputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body, IStepExecutionContext context)
        {
            //TODO: Move to own class
            foreach (var input in step.Inputs)
            {
                var member = (input.Target.Body as MemberExpression);
                object resolvedValue = null;

                switch (input.Source.Parameters.Count)
                {
                    case 1:
                        resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data);
                        break;
                    case 2:
                        resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data, context);
                        break;
                    default:
                        throw new ArgumentException();
                }

                step.BodyType.GetProperty(member.Member.Name).SetValue(body, resolvedValue);
            }
        }

        private void ProcessOutputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body, IStepExecutionContext context)
        {
            foreach (var output in step.Outputs)
            {
                var resolvedValue = output.Source.Compile().DynamicInvoke(body);
                var data = workflow.Data;
                var setter = ExpressionHelpers.CreateSetter(output.Target);
                var targetType = setter.Parameters.Last().Type;

                var convertedValue = resolvedValue;
                // We need to make sure the resolvedValue is of the correct type.
                // However if the targetType is object we don't need to do anything and in some cases Convert.ChangeType will throw.
                if (targetType != typeof(object))
                {
                    convertedValue = Convert.ChangeType(resolvedValue, targetType);
                }

                if (setter.Parameters.Count == 2)
                {
                    setter.Compile().DynamicInvoke(data, convertedValue);
                }
                else
                {
                    setter.Compile().DynamicInvoke(data, context, convertedValue);
                }
            }
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
                    if (!workflow.ExecutionPointers.Where(x => x.Scope.Contains(pointer.Id)).All(x => x.EndTime.HasValue)) 
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
        }
        
    }
}
