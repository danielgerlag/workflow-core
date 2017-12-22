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

        private readonly IDateTimeProvider _datetimeProvider;
        private readonly ILogger _logger;

        public ExecutionResultProcessor(IDateTimeProvider datetimeProvider, ILoggerFactory loggerFactory)
        {
            _datetimeProvider = datetimeProvider;
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
                    var nextId = Guid.NewGuid().ToString();
                    workflow.ExecutionPointers.Add(new ExecutionPointer()
                    {
                        Id = nextId,
                        PredecessorId = pointer.Id,
                        StepId = outcomeTarget.NextStep,
                        Active = true,
                        ContextItem = pointer.ContextItem,
                        Status = PointerStatus.Pending,
                        StepName = def.Steps.First(x => x.Id == outcomeTarget.NextStep).Name,
                        Scope = new Stack<string>(pointer.Scope)
                    });

                    pointer.SuccessorIds.Add(nextId);
                }
            }
            else
            {
                foreach (var branch in result.BranchValues)
                {
                    foreach (var childDefId in step.Children)
                    {
                        var childPointerId = Guid.NewGuid().ToString();
                        var childScope = new Stack<string>(pointer.Scope);
                        childScope.Push(pointer.Id);
                        workflow.ExecutionPointers.Add(new ExecutionPointer()
                        {
                            Id = childPointerId,
                            PredecessorId = pointer.Id,
                            StepId = childDefId,
                            Active = true,
                            ContextItem = branch,
                            Status = PointerStatus.Pending,
                            StepName = def.Steps.First(x => x.Id == childDefId).Name,
                            Scope = childScope
                        });

                        pointer.Children.Add(childPointerId);
                    }
                }
            }
        }

        public void HandleStepException(WorkflowInstance workflow, WorkflowOptions options, WorkflowExecutorResult wfResult, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception ex)
        {
            pointer.RetryCount++;
            pointer.Status = PointerStatus.Failed;
            
            var compensatingStepId = FindScopeCompensationStepId(workflow, def, pointer);

            switch (step.ErrorBehavior ?? (compensatingStepId.HasValue ? WorkflowErrorHandling.Compensate : def.DefaultErrorBehavior))
            {
                case WorkflowErrorHandling.Retry:
                    pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? options.ErrorRetryInterval);
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

        private void AddCompensationPointer(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, ExecutionPointer exceptionPointer, int compensationStepId)
        {
            var nextId = Guid.NewGuid().ToString();
            workflow.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = nextId,
                PredecessorId = exceptionPointer.Id,
                StepId = compensationStepId,
                Active = true,
                ContextItem = pointer.ContextItem,
                Status = PointerStatus.Pending,
                StepName = def.Steps.First(x => x.Id == compensationStepId).Name,
                Scope = new Stack<string>(pointer.Scope)
            });

            exceptionPointer.SuccessorIds.Add(nextId);
        }

        private void Compensate(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer exceptionPointer)
        {            
            var scope = new Stack<string>(exceptionPointer.Scope);
            scope.Push(exceptionPointer.Id);

            exceptionPointer.Active = false;
            exceptionPointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
            exceptionPointer.Status = PointerStatus.Failed;

            while (scope.Count > 0)
            {
                var pointerId = scope.Pop();
                var pointer = workflow.ExecutionPointers.First(x => x.Id == pointerId);
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                                
                if (step.CompensationStepId.HasValue)
                {
                    pointer.Active = false;
                    pointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
                    pointer.Status = PointerStatus.Failed;

                    AddCompensationPointer(workflow, def, pointer, exceptionPointer, step.CompensationStepId.Value);                    

                    foreach (var outcomeTarget in step.Outcomes.Where(x => x.GetValue(workflow.Data) == null))
                    {
                        var nextId = Guid.NewGuid().ToString();
                        workflow.ExecutionPointers.Add(new ExecutionPointer()
                        {
                            Id = nextId,
                            PredecessorId = pointer.Id,
                            StepId = outcomeTarget.NextStep,
                            Active = true,
                            ContextItem = pointer.ContextItem,
                            Status = PointerStatus.Pending,
                            StepName = def.Steps.First(x => x.Id == outcomeTarget.NextStep).Name,
                            Scope = new Stack<string>(pointer.Scope)
                        });

                        pointer.SuccessorIds.Add(nextId);
                    }

                }
                                
                var prevSiblings = workflow.ExecutionPointers.Where(x => pointer.Scope.SequenceEqual(x.Scope) && x.Id != pointer.Id && x.Status == PointerStatus.Complete).ToList();
                foreach (var siblingPointer in prevSiblings)
                {
                    var siblingStep = def.Steps.First(x => x.Id == siblingPointer.StepId);
                    if (siblingStep.CompensationStepId.HasValue)
                        AddCompensationPointer(workflow, def, siblingPointer, exceptionPointer, siblingStep.CompensationStepId.Value);
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