using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class RetryHandler : IWorkflowErrorHandler
    {
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly WorkflowOptions _options;
        public WorkflowErrorHandling Type => WorkflowErrorHandling.Retry;

        public RetryHandler(IDateTimeProvider datetimeProvider, WorkflowOptions options)
        {
            _datetimeProvider = datetimeProvider;
            _options = options;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            pointer.RetryCount++;
            pointer.SleepUntil = _datetimeProvider.UtcNow.Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? _options.ErrorRetryInterval);
            step.PrimeForRetry(pointer);
        }
    }
}
