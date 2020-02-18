using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class CatchHandler : IWorkflowErrorHandler
    {
        private readonly ILifeCycleEventPublisher _eventPublisher;
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly WorkflowOptions _options;
        
        public CatchHandler(IExecutionPointerFactory pointerFactory, ILifeCycleEventPublisher eventPublisher, IDateTimeProvider datetimeProvider, WorkflowOptions options)
        {
            _pointerFactory = pointerFactory;
            _eventPublisher = eventPublisher;
            _datetimeProvider = datetimeProvider;
            _options = options;
        }
        
        public WorkflowErrorHandling Type => WorkflowErrorHandling.Catch; 

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer exceptionPointer, WorkflowStep step,
            Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            var scope = new Stack<string>(exceptionPointer.Scope.Reverse());
            scope.Push(exceptionPointer.Id);

            var exceptionCaught = false;

            while (scope.Any())
            {
                var pointerId = scope.Pop();
                var scopePointer = workflow.ExecutionPointers.FindById(pointerId);
                var scopeStep = def.Steps.FindById(scopePointer.StepId);
                
                if ((scopeStep.ErrorBehavior ?? WorkflowErrorHandling.Catch) != WorkflowErrorHandling.Catch)
                {
                    bubbleUpQueue.Enqueue(scopePointer);
                    continue;
                } //TODO: research if it's needed
                
                scopePointer.Active = false;
                scopePointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
                scopePointer.Status = PointerStatus.Failed;

                while (scopeStep.CatchStepsQueue.Count != 0)
                {
                    var nextCatchStepPair = scopeStep.CatchStepsQueue.Dequeue();
                    var exceptionType = nextCatchStepPair.Key;
                    var catchStepId = nextCatchStepPair.Value;
                    if (exceptionType.IsInstanceOfType(exception))
                    {
                        var catchPointer = _pointerFactory.BuildCatchPointer(def, scopePointer, exceptionPointer, catchStepId, exception);
                        workflow.ExecutionPointers.Add(catchPointer);
                        
                        foreach (var outcomeTarget in scopeStep.Outcomes.Where(x => x.Matches(workflow.Data)))
                            workflow.ExecutionPointers.Add(_pointerFactory.BuildNextPointer(def, scopePointer, outcomeTarget));

                        exceptionCaught = true;
                        
                        scopeStep.CatchStepsQueue.Clear();
                        scope.Clear();
                        break;
                    }
                }
            }

            if (!exceptionCaught)
            {
                workflow.Status = WorkflowStatus.Terminated;
                _eventPublisher.PublishNotification(new WorkflowTerminated()
                {
                    EventTimeUtc = _datetimeProvider.UtcNow,
                    Reference = workflow.Reference,
                    WorkflowInstanceId = workflow.Id,
                    WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                    Version = workflow.Version,
                    Exception = exception
                });   
            }
        }
    }
}