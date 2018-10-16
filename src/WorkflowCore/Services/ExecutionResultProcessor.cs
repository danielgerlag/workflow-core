using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ExecutionResultProcessor : IExecutionResultProcessor
    {
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;

        public ExecutionResultProcessor(IExecutionPointerFactory pointerFactory, IDateTimeProvider datetimeProvider, WorkflowOptions options, ILoggerFactory loggerFactory)
        {
            _pointerFactory = pointerFactory;
            _datetimeProvider = datetimeProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<ExecutionResultProcessor>();
        }

        public void ProcessExecutionResult(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, ExecutionResult result, WorkflowExecutorResult workflowResult)
        {
            pointer.PersistenceData = result.PersistenceData;
            pointer.Outcome = result.OutcomeValue;
            if (result.SleepFor.HasValue)
            {
                pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(result.SleepFor.Value);
                pointer.Status = PointerStatus.Sleeping;
            }

            if (!string.IsNullOrEmpty(result.EventName))
            {
                pointer.EventName = result.EventName;
                pointer.EventKey = result.EventKey;
                pointer.Active = false;
                pointer.Status = PointerStatus.WaitingForEvent;

                workflowResult.Subscriptions.Add(new EventSubscription()
                {
                    WorkflowId = workflow.Id,
                    StepId = pointer.StepId,
                    EventName = pointer.EventName,
                    EventKey = pointer.EventKey,
                    SubscribeAsOf = result.EventAsOf
                });
            }

            if (result.Proceed)
            {
                pointer.Active = false;
                pointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
                pointer.Status = PointerStatus.Complete;

                foreach (var outcomeTarget in step.Outcomes.Where(x => object.Equals(x.GetValue(workflow.Data), result.OutcomeValue) || x.GetValue(workflow.Data) == null))
                {                    
                    workflow.ExecutionPointers.Add(_pointerFactory.BuildNextPointer(def, pointer, outcomeTarget));
                }
            }
            else
            {
                foreach (var branch in result.BranchValues)
                {
                    foreach (var childDefId in step.Children)
                    {   
                        workflow.ExecutionPointers.Add(_pointerFactory.BuildChildPointer(def, pointer, childDefId, branch));                        
                    }
                }
            }
        }

        public void HandleStepException(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step)
        {            
            pointer.Status = PointerStatus.Failed;            
            var compensatingStepId = FindScopeCompensationStepId(workflow, def, pointer);
            var errorOption = (step.ErrorBehavior ?? (compensatingStepId.HasValue ? WorkflowErrorHandling.Compensate : def.DefaultErrorBehavior));
            SelectErrorStrategy(errorOption, workflow, def, pointer, step);
        }

        private void SelectErrorStrategy(WorkflowErrorHandling errorOption, WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step)
        {
            switch (errorOption)
            {
                case WorkflowErrorHandling.Retry:
                    pointer.RetryCount++;
                    pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? _options.ErrorRetryInterval);
                    step.PrimeForRetry(pointer);
                    break;
                case WorkflowErrorHandling.Suspend:
                    workflow.Status = WorkflowStatus.Suspended;
                    break;
                case WorkflowErrorHandling.Terminate:
                    workflow.Status = WorkflowStatus.Terminated;
                    break;
                case WorkflowErrorHandling.Compensate:
                    Compensate(workflow, def, pointer);
                    break;
            }
        }
        
        private void Compensate(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer exceptionPointer)
        {            
            var scope = new Stack<string>(exceptionPointer.Scope);
            scope.Push(exceptionPointer.Id);

            exceptionPointer.Active = false;
            exceptionPointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
            exceptionPointer.Status = PointerStatus.Failed;

            while (scope.Any())
            {
                var pointerId = scope.Pop();
                var pointer = workflow.ExecutionPointers.First(x => x.Id == pointerId);
                var step = def.Steps.First(x => x.Id == pointer.StepId);

                var resume = true;
                var revert = false;

                if (scope.Any())
                {
                    var parentId = scope.Peek();
                    var parentPointer = workflow.ExecutionPointers.First(x => x.Id == parentId);
                    var parentStep = def.Steps.First(x => x.Id == parentPointer.StepId);
                    resume = parentStep.ResumeChildrenAfterCompensation;
                    revert = parentStep.RevertChildrenAfterCompensation;
                }

                if ((step.ErrorBehavior ?? WorkflowErrorHandling.Compensate) != WorkflowErrorHandling.Compensate)
                {
                    SelectErrorStrategy(step.ErrorBehavior ?? WorkflowErrorHandling.Retry, workflow, def, pointer, step);
                    continue;
                }

                if (step.CompensationStepId.HasValue)
                {
                    pointer.Active = false;
                    pointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
                    pointer.Status = PointerStatus.Compensated;

                    var compensationPointer = _pointerFactory.BuildCompensationPointer(def, pointer, exceptionPointer, step.CompensationStepId.Value);
                    workflow.ExecutionPointers.Add(compensationPointer);
                    
                    if (resume)
                    {
                        foreach (var outcomeTarget in step.Outcomes.Where(x => x.GetValue(workflow.Data) == null))
                            workflow.ExecutionPointers.Add(_pointerFactory.BuildNextPointer(def, pointer, outcomeTarget));
                    }
                }

                if (revert)
                {
                    var prevSiblings = workflow.ExecutionPointers
                        .Where(x => pointer.Scope.SequenceEqual(x.Scope) && x.Id != pointer.Id && x.Status == PointerStatus.Complete)
                        .OrderByDescending(x => x.EndTime)
                        .ToList();

                    foreach (var siblingPointer in prevSiblings)
                    {
                        var siblingStep = def.Steps.First(x => x.Id == siblingPointer.StepId);
                        if (siblingStep.CompensationStepId.HasValue)
                        {
                            var compensationPointer = _pointerFactory.BuildCompensationPointer(def, siblingPointer, exceptionPointer, siblingStep.CompensationStepId.Value);
                            workflow.ExecutionPointers.Add(compensationPointer);
                            siblingPointer.Status = PointerStatus.Compensated;
                        }
                    }
                }
            }
        }

        private int? FindScopeCompensationStepId(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer currentPointer)
        {
            var scope = new Stack<string>(currentPointer.Scope);
            scope.Push(currentPointer.Id);

            while (scope.Count > 0)
            {
                var pointerId = scope.Pop();
                var pointer = workflow.ExecutionPointers.First(x => x.Id == pointerId);
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step.CompensationStepId.HasValue)
                    return step.CompensationStepId.Value;
            }

            return null;
        }
    }
}