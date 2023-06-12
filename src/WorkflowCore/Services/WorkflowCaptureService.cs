using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class WorkflowCaptureService : IWorkflowCaptureService, IDisposable
    {
        private readonly IWorkflowHost _host;
        private readonly Dictionary<string, TaskCompletionSource<object>> _completionSources = new Dictionary<string, TaskCompletionSource<object>>();

        public WorkflowCaptureService(IWorkflowHost host)
        {
            _host = host;

            _host.OnStepError += StepErrorHandler;
            _host.OnLifeCycleEvent += LifeCycleEventHandler;
        }

        private void LifeCycleEventHandler(LifeCycleEvent evt)
        {
            if (!_completionSources.TryGetValue(evt.WorkflowInstanceId, out var completionSource))
                return;
            
            if (evt is WorkflowCompleted)
            {
                completionSource.SetResult(true);
            }
        }

        private void StepErrorHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            if (_completionSources.TryGetValue(workflow.Id, out var taskCompletionSource))
            {
                taskCompletionSource.SetException(exception);
            }
        }

        public async Task<PendingActivity> CaptureActivity(string activity, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowCompletionTask = CaptureWorkflowExceptions(workflowInstanceId, cancellationToken);
            var pendingActivityTask = _host.GetPendingActivity(activity, "worker-1", workflowInstanceId, cancellationToken);

            var completedTask = await Task.WhenAny(pendingActivityTask, workflowCompletionTask);

            if (completedTask == workflowCompletionTask)
            {
                completedTask.GetAwaiter().GetResult();

                throw new InvalidOperationException("Workflow completed without creating the activity");
            }

            var pendingActivity = pendingActivityTask.GetAwaiter().GetResult();
            if (pendingActivity == null)
            {
                throw new InvalidOperationException("Couldn't retrieve the activity");
            }

            return pendingActivity;
        }

        public async Task CaptureWorkflowExceptions(string workflowInstanceId, CancellationToken cancellationToken = default)
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
                completedTask.GetAwaiter().GetResult();
            }
            finally
            {
                _completionSources.Remove(workflowInstanceId);
            }
        }

        public void Dispose()
        {
            _host.OnStepError -= StepErrorHandler;
            _host.OnLifeCycleEvent -= LifeCycleEventHandler;
        }
    }
}