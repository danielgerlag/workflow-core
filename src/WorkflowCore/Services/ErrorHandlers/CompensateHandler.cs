using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class CompensateHandler : IWorkflowErrorHandler
    {
        private readonly ILifeCycleEventPublisher _eventPublisher;
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly WorkflowOptions _options;

        public WorkflowErrorHandling Type => WorkflowErrorHandling.Compensate;

        public CompensateHandler(IExecutionPointerFactory pointerFactory, ILifeCycleEventPublisher eventPublisher, IDateTimeProvider datetimeProvider, WorkflowOptions options)
        {
            _pointerFactory = pointerFactory;
            _eventPublisher = eventPublisher;
            _datetimeProvider = datetimeProvider;
            _options = options;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer exceptionPointer, WorkflowStep exceptionStep, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            var scope = new Stack<string>(exceptionPointer.Scope);
            scope.Push(exceptionPointer.Id);

            exceptionPointer.Active = false;
            exceptionPointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
            exceptionPointer.Status = PointerStatus.Failed;

            while (scope.Any())
            {
                var pointerId = scope.Pop();
                var scopePointer = workflow.ExecutionPointers.FindById(pointerId);
                var scopeStep = def.Steps.First(x => x.Id == scopePointer.StepId);

                var resume = true;
                var revert = false;

                if (scope.Any())
                {
                    var parentId = scope.Peek();
                    var parentPointer = workflow.ExecutionPointers.FindById(parentId);
                    var parentStep = def.Steps.First(x => x.Id == parentPointer.StepId);
                    resume = parentStep.ResumeChildrenAfterCompensation;
                    revert = parentStep.RevertChildrenAfterCompensation;
                }

                if ((scopeStep.ErrorBehavior ?? WorkflowErrorHandling.Compensate) != WorkflowErrorHandling.Compensate)
                {
                    bubbleUpQueue.Enqueue(scopePointer);
                    continue;
                }

                if (scopeStep.CompensationStepId.HasValue)
                {
                    scopePointer.Active = false;
                    scopePointer.EndTime = _datetimeProvider.Now.ToUniversalTime();
                    scopePointer.Status = PointerStatus.Compensated;

                    var compensationPointer = _pointerFactory.BuildCompensationPointer(def, scopePointer, exceptionPointer, scopeStep.CompensationStepId.Value);
                    workflow.ExecutionPointers.Add(compensationPointer);

                    if (resume)
                    {
                        foreach (var outcomeTarget in scopeStep.Outcomes.Where(x => x.GetValue(workflow.Data) == null))
                            workflow.ExecutionPointers.Add(_pointerFactory.BuildNextPointer(def, scopePointer, outcomeTarget));
                    }
                }

                if (revert)
                {
                    var prevSiblings = workflow.ExecutionPointers
                        .Where(x => scopePointer.Scope.SequenceEqual(x.Scope) && x.Id != scopePointer.Id && x.Status == PointerStatus.Complete)
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
    }
}
