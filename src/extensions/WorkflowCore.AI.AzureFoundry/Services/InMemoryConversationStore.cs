using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// In-memory implementation of conversation store (for development/testing)
    /// </summary>
    public class InMemoryConversationStore : IConversationStore
    {
        private readonly ConcurrentDictionary<string, ConversationThread> _threads = 
            new ConcurrentDictionary<string, ConversationThread>();

        private readonly ConcurrentDictionary<string, string> _workflowThreadMap = 
            new ConcurrentDictionary<string, string>();

        public Task<ConversationThread> GetThreadAsync(string threadId)
        {
            _threads.TryGetValue(threadId, out var thread);
            return Task.FromResult(thread);
        }

        public Task<ConversationThread> GetOrCreateThreadAsync(string workflowInstanceId, string executionPointerId)
        {
            var key = $"{workflowInstanceId}:{executionPointerId}";

            var threadId = _workflowThreadMap.GetOrAdd(key, k =>
            {
                var thread = new ConversationThread
                {
                    WorkflowInstanceId = workflowInstanceId,
                    ExecutionPointerId = executionPointerId
                };
                _threads[thread.Id] = thread;
                return thread.Id;
            });

            if (_threads.TryGetValue(threadId, out var existingThread))
            {
                return Task.FromResult(existingThread);
            }

            // The thread was removed or the mapping is stale; recreate and update the mapping.
            var newThread = new ConversationThread
            {
                WorkflowInstanceId = workflowInstanceId,
                ExecutionPointerId = executionPointerId
            };

            _threads[newThread.Id] = newThread;
            _workflowThreadMap[key] = newThread.Id;

            return Task.FromResult(newThread);
        }

        public Task SaveThreadAsync(ConversationThread thread)
        {
            _threads[thread.Id] = thread;
            
            if (!string.IsNullOrEmpty(thread.WorkflowInstanceId) && !string.IsNullOrEmpty(thread.ExecutionPointerId))
            {
                var key = $"{thread.WorkflowInstanceId}:{thread.ExecutionPointerId}";
                _workflowThreadMap[key] = thread.Id;
            }

            return Task.CompletedTask;
        }

        public Task DeleteThreadAsync(string threadId)
        {
            if (_threads.TryRemove(threadId, out var thread))
            {
                if (!string.IsNullOrEmpty(thread.WorkflowInstanceId) && !string.IsNullOrEmpty(thread.ExecutionPointerId))
                {
                    var key = $"{thread.WorkflowInstanceId}:{thread.ExecutionPointerId}";
                    _workflowThreadMap.TryRemove(key, out _);
                }
            }

            return Task.CompletedTask;
        }
    }
}
