using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

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

            while (scope.Any())
            {
                var pointerId = scope.Pop();
                var scopePointer = workflow.ExecutionPointers.FindById(pointerId);
                var scopeStep = def.Steps.FindById(scopePointer.StepId);
                if ((scopeStep.ErrorBehavior ?? WorkflowErrorHandling.Catch) != WorkflowErrorHandling.Catch)
                {
                    bubbleUpQueue.Enqueue(scopePointer);
                    continue;
                }
                
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
                        scopeStep.CatchStepsQueue.Clear();
                        scope.Clear();
                        break;
                    }
                }
            }
        }
    }
}