using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Abstraction for storing and retrieving conversation threads
    /// </summary>
    public interface IConversationStore
    {
        /// <summary>
        /// Get a conversation thread by ID
        /// </summary>
        Task<ConversationThread> GetThreadAsync(string threadId);

        /// <summary>
        /// Get or create a thread for a workflow execution pointer
        /// </summary>
        Task<ConversationThread> GetOrCreateThreadAsync(string workflowInstanceId, string executionPointerId);

        /// <summary>
        /// Save a conversation thread
        /// </summary>
        Task SaveThreadAsync(ConversationThread thread);

        /// <summary>
        /// Delete a conversation thread
        /// </summary>
        Task DeleteThreadAsync(string threadId);
    }
}
