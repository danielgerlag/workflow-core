using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class WorkflowCaptureService : IWorkflowCaptureService, IDisposable
    {
        private readonly IWorkflowHost _host;
        private readonly IPersistenceProvider _persistence;
        private readonly Dictionary<string, TaskCompletionSource<WorkflowInstance>> _completionSources = new Dictionary<string, TaskCompletionSource<WorkflowInstance>>();

        public WorkflowCaptureService(IWorkflowHost host, IPersistenceProvider persistence)
        {
            _host = host;
            _persistence = persistence;

            _host.OnStepError += StepErrorHandler;
            _host.OnLifeCycleEvent += LifeCycleEventHandler;
        }

        private void LifeCycleEventHandler(LifeCycleEvent evt)
        {
            if (!_completionSources.TryGetValue(evt.WorkflowInstanceId, out var completionSource))
                return;
            
            if (evt is WorkflowCompleted)
            {
                completionSource.SetResult(evt.Workflow);
            }
        }

        private void StepErrorHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            if (_completionSources.TryGetValue(workflow.Id, out var taskCompletionSource))
            {
                taskCompletionSource.SetException(exception);
            }
        }

        public async Task<PendingActivity> CaptureActivity(string workflowId, CancellationToken cancellationToken = default)
        {
            var workflowCompletionTask = CaptureWorkflowCompletion(workflowId, cancellationToken);
            var pendingActivityTask = _host.GetFirstPendingActivity("worker-1", workflowId, cancellationToken);

            var completedTask = await Task.WhenAny(pendingActivityTask, workflowCompletionTask);

            if (completedTask == workflowCompletionTask)
            {
                completedTask.GetAwaiter().GetResult();

                return null;
            }

            var pendingActivity = pendingActivityTask.GetAwaiter().GetResult();

            return pendingActivity;
        }

        public async Task<WorkflowInstance> CaptureWorkflowCompletion(string workflowId, CancellationToken cancellationToken = default)
        {
            try
            {
                // todo: lock if needed
                if (!_completionSources.TryGetValue(workflowId, out var completionSource))
                {
                    completionSource = new TaskCompletionSource<WorkflowInstance>();
                    _completionSources.Add(workflowId, completionSource);
                }

                var workflow = await _persistence.GetWorkflowInstance(workflowId, cancellationToken);
                if (workflow.Status != WorkflowStatus.Runnable)
                {
                    return workflow;
                }
                
                var cancelledTaskCompletionSource = new TaskCompletionSource<WorkflowInstance>();

                cancellationToken.Register(() => cancelledTaskCompletionSource.TrySetCanceled());

                var completedTask = await Task.WhenAny(cancelledTaskCompletionSource.Task, completionSource.Task);
                return completedTask.GetAwaiter().GetResult();
            }
            finally
            {
                _completionSources.Remove(workflowId);
            }
        }

        public void Dispose()
        {
            _host.OnStepError -= StepErrorHandler;
            _host.OnLifeCycleEvent -= LifeCycleEventHandler;
        }
    }
}