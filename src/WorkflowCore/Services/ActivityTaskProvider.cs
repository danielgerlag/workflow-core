using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ActivityTaskProvider : IActivityTaskProvider
    {
        private readonly IWorkflowHost _host;
        private readonly Dictionary<string, TaskCompletionSource<object>> _completionSources = new Dictionary<string, TaskCompletionSource<object>>();

        public ActivityTaskProvider(IWorkflowHost host)
        {
            _host = host;
            _host.OnStepError += StepErrorHandler;
        }

        private void StepErrorHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            if (_completionSources.TryGetValue(workflow.Id, out var taskCompletionSource))
            {
                taskCompletionSource.SetException(exception);
            }
        }

        public async Task WaitForActivityCreation(string activity, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowCompletionTask = WaitForWorkflowEnd(workflowInstanceId, cancellationToken);
            var pendingActivityTask = _host.GetPendingActivity(activity, "worker-1", workflowInstanceId, cancellationToken);

            var completedTask = await Task.WhenAny(pendingActivityTask, workflowCompletionTask);
            await completedTask;
            
            if (pendingActivityTask == null)
            {
                throw new InvalidOperationException("Couldn't retrieve the activity");
            }
        }

        private async Task WaitForWorkflowEnd(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            try
            {
                // todo: lock if needed
                if (!_completionSources.TryGetValue(workflowInstanceId, out var completionSource))
                {
                    completionSource = new TaskCompletionSource<object>();
                    _completionSources.Add(workflowInstanceId, completionSource);
                }
                
                var cancelledTaskCompletionSource = new TaskCompletionSource<object>();

                cancellationToken.Register(() => cancelledTaskCompletionSource.TrySetCanceled());

                var completedTask = await Task.WhenAny(cancelledTaskCompletionSource.Task, completionSource.Task);
                completedTask.GetAwaiter().GetResult(); // This is needed for rethrowing the exception only
            }
            finally
            {
                _completionSources.Remove(workflowInstanceId);
            }
        }
    }
}