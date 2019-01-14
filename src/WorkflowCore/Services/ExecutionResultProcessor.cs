﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class ExecutionResultProcessor : IExecutionResultProcessor
    {
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly ILogger _logger;
        private readonly ILifeCycleEventPublisher _eventPublisher;
        private readonly IEnumerable<IWorkflowErrorHandler> _errorHandlers;
        private readonly WorkflowOptions _options;

        public ExecutionResultProcessor(IExecutionPointerFactory pointerFactory, IDateTimeProvider datetimeProvider, ILifeCycleEventPublisher eventPublisher, IEnumerable<IWorkflowErrorHandler> errorHandlers, WorkflowOptions options, ILoggerFactory loggerFactory)
        {
            _pointerFactory = pointerFactory;
            _datetimeProvider = datetimeProvider;
            _eventPublisher = eventPublisher;
            _errorHandlers = errorHandlers;
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

                _eventPublisher.PublishNotification(new StepCompleted()
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

        public void HandleStepException(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception)
        {
            _eventPublisher.PublishNotification(new WorkflowError()
            {
                EventTimeUtc = _datetimeProvider.Now,
                Reference = workflow.Reference,
                WorkflowInstanceId = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version,
                ExecutionPointerId = pointer.Id,
                StepId = step.Id,
                Message = exception.Message
            });
            pointer.Status = PointerStatus.Failed;
            
            var queue = new Queue<ExecutionPointer>();
            queue.Enqueue(pointer);

            while (queue.Count > 0)
            {
                var exceptionPointer = queue.Dequeue();
                var exceptionStep = def.Steps.Find(x => x.Id == exceptionPointer.StepId);
                var compensatingStepId = FindScopeCompensationStepId(workflow, def, exceptionPointer);
                var errorOption = (exceptionStep.ErrorBehavior ?? (compensatingStepId.HasValue ? WorkflowErrorHandling.Compensate : def.DefaultErrorBehavior));

                foreach (var handler in _errorHandlers.Where(x => x.Type == errorOption))
                {
                    handler.Handle(workflow, def, exceptionPointer, exceptionStep, exception, queue);
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
                var pointer = workflow.ExecutionPointers.FindById(pointerId);
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step.CompensationStepId.HasValue)
                    return step.CompensationStepId.Value;
            }

            return null;
        }
    }
}