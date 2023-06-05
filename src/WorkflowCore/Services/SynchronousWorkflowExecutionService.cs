using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

// This is the modified version of the following gist https://gist.github.com/kpko/f4c10ae7646d58038e0137278e6f49f9
namespace WorkflowCore.Services
{
    public class SynchronousWorkflowExecutionResult
    {
        public string WorkflowId { get; set; }
        public string WorkflowInstanceId { get; set; }
        public string Reference { get; set; }
        public LifeCycleEvent LastLifeCycleEvent { get; set; }
        public Task WorkflowCompletionTask { get; set; }
    }
    
    public class SynchronousWorkflowExecutionService : ISynchronousWorkflowExecutionService
    {
        private readonly IWorkflowHost _host;
        private readonly ILifeCycleEventHub _hub;
        private readonly IPersistenceProvider _persistenceProvider;

        private readonly Dictionary<string, TaskCompletionSource<object>>
            _completionSources = new Dictionary<string, TaskCompletionSource<object>>();

        public SynchronousWorkflowExecutionService(IWorkflowHost host, ILifeCycleEventHub hub)
        {
            _host = host;

            _hub = hub;
            _hub.Subscribe(HandleWorkflowEvent);
        }

        private void HandleWorkflowEvent(LifeCycleEvent @event)
        {
            switch (@event)
            {
                case WorkflowCompleted completed:
                case WorkflowTerminated terminated:
                case WorkflowError error:
                    if (_completionSources.TryGetValue(@event.WorkflowInstanceId, out var taskCompletionSource))
                    {
                        var result = (SynchronousWorkflowExecutionResult)taskCompletionSource.Task.AsyncState;
                        result.LastLifeCycleEvent = @event;
                        taskCompletionSource.SetResult(result);
                    }

                    break;
            }
        }

        public async Task<SynchronousWorkflowExecutionResult> StartWorkflowAsync<TData>(string workflowId,
            int? version = null, TData data = null, string reference = null) where TData : class, new()
        {
            var result = new SynchronousWorkflowExecutionResult
            {
                WorkflowId = workflowId,
                Reference = reference
            };

            var completionSource = new TaskCompletionSource<object>(result);
            var instanceId = await _host.StartWorkflow(workflowId, version, data, reference);
            result.WorkflowInstanceId = instanceId;

            _completionSources.Add(instanceId, completionSource);
            result.WorkflowCompletionTask = completionSource.Task;

            return result;
        }
        
        /// <summary>
        /// Executes the workflow steps before the activity
        /// </summary>
        /// <returns>The last outcome of the steps. This can be null</returns>
        public async Task<object> RunWorkflowUntilActivityAsync<TData>(string workflowId, string activity, int? version = null, TData data = null, string reference = null, CancellationToken cancellationToken = default) where TData : class, new()
        {
            var executionResult = await StartWorkflowAsync(workflowId, version, data, reference);
            var activityTask = _host.GetPendingActivity(activity, workflowId);
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

            var completedTask = await Task.WhenAny(executionResult.WorkflowCompletionTask, activityTask, cancellationTask);
            if (completedTask == executionResult.WorkflowCompletionTask)
            {
                throw new InvalidOperationException("Workflow completed without completing the specified activity");
            }

            var workflowInstance = await _persistenceProvider.GetWorkflowInstance(executionResult.WorkflowInstanceId, cancellationToken);
            var lastPointerWithOutcome = workflowInstance.ExecutionPointers.LastOrDefault(p => p.Outcome != null);
            return lastPointerWithOutcome?.Outcome;
        }
    }
}