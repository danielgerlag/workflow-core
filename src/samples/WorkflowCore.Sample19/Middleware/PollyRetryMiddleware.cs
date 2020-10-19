using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19.Middleware
{
    public class PollyRetryMiddleware : IWorkflowStepMiddleware
    {
        private const string StepContextKey = "WorkflowStepContext";
        private const int MaxRetries = 3;
        private readonly ILogger<PollyRetryMiddleware> _log;

        public PollyRetryMiddleware(ILogger<PollyRetryMiddleware> log)
        {
            _log = log;
        }

        public IAsyncPolicy<ExecutionResult> GetRetryPolicy() =>
            Policy<ExecutionResult>
                .Handle<TimeoutException>()
                .RetryAsync(
                    MaxRetries,
                    (result, retryCount, context) =>
                        UpdateRetryCount(result.Exception, retryCount, context[StepContextKey] as IStepExecutionContext)
                );

        public async Task<ExecutionResult> HandleAsync(
            IStepExecutionContext context,
            IStepBody body,
            WorkflowStepDelegate next
        )
        {
            return await GetRetryPolicy().ExecuteAsync(ctx => next(), new Dictionary<string, object>
            {
                { StepContextKey, context }
            });
        }

        private Task UpdateRetryCount(
            Exception exception,
            int retryCount,
            IStepExecutionContext stepContext)
        {
            var stepInstance = stepContext.ExecutionPointer;
            stepInstance.RetryCount = retryCount;

            _log.LogWarning(
                exception,
                "Exception occurred in step {StepId}. Retrying [{RetryCount}/{MaxCount}]",
                stepInstance.Id,
                retryCount,
                MaxRetries
            );

            // TODO: Come up with way to persist workflow
            return Task.CompletedTask;
        }
    }
}
