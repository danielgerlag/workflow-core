using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class WorkflowWaitTaskStore : IWorkflowWaitTaskStore
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _tasks;

        public WorkflowWaitTaskStore()
        {
            _tasks = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        }

        public void AddTask(string workflowId)
        {
            var token = new TaskCompletionSource<bool>();
            _tasks.TryAdd(workflowId, token);
        }

        public Task RemoveTask(string workflowId)
        {
            if (_tasks.TryRemove(workflowId, out TaskCompletionSource<bool> taskSource))
            {
                taskSource.SetResult(true);
            }

            return Task.CompletedTask;
        }

        public Task Wait(string workflowId)
        {
            if (_tasks.TryGetValue(workflowId, out TaskCompletionSource<bool> taskSource))
            {
                return taskSource.Task;
            }
            else
            {
                // Maybe raise an exception?
                return Task.CompletedTask;
            }
        }

        public Task Wait(string workflowId, TimeSpan timeout)
        {
            if (_tasks.TryGetValue(workflowId, out TaskCompletionSource<bool> taskSource))
            {
                Task.Delay(timeout);
                return Task.WhenAny(taskSource.Task, Task.Delay(timeout));
            }
            else
            {
                // Maybe raise an exception?
                return Task.CompletedTask;
            }
        }
    }
}