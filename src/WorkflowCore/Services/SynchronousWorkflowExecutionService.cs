using System.Collections.Generic;
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
    }
    
    public class SynchronousWorkflowExecutionService : ISynchronousWorkflowExecutionService
    {
        private readonly IWorkflowHost _host;
        private readonly ILifeCycleEventHub _hub;

        private readonly Dictionary<string, TaskCompletionSource<SynchronousWorkflowExecutionResult>>
            _completionSources = new Dictionary<string, TaskCompletionSource<SynchronousWorkflowExecutionResult>>();

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

        public async Task<SynchronousWorkflowExecutionResult> StartWorkflowAndWait<TData>(string workflowId,
            int? version = null, TData data = null, string reference = null) where TData : class, new()
        {
            var result = new SynchronousWorkflowExecutionResult()
            {
                WorkflowId = workflowId,
                Reference = reference
            };

            var completionSource = new TaskCompletionSource<SynchronousWorkflowExecutionResult>(result);
            var instanceId = await _host.StartWorkflow(workflowId, version, data, reference);
            result.WorkflowInstanceId = instanceId;

            _completionSources.Add(instanceId, completionSource);
            return await completionSource.Task;
        }
    }
}