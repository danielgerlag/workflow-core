using System;
using System.Collections.Generic;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Represents a conversation thread containing multiple messages
    /// </summary>
    public class ConversationThread
    {
        /// <summary>
        /// Unique identifier for this conversation thread
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Associated workflow instance ID
        /// </summary>
        public string WorkflowInstanceId { get; set; }

        /// <summary>
        /// Associated execution pointer ID
        /// </summary>
        public string ExecutionPointerId { get; set; }

        /// <summary>
        /// Messages in the conversation
        /// </summary>
        public IList<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

        /// <summary>
        /// When the thread was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the thread was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for the thread
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Total token count across all messages
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// Add a message to the thread
        /// </summary>
        public void AddMessage(ConversationMessage message)
        {
            Messages.Add(message);
            UpdatedAt = DateTime.UtcNow;
            if (message.TokenCount.HasValue)
            {
                TotalTokens += message.TokenCount.Value;
            }
        }

        /// <summary>
        /// Add a system message
        /// </summary>
        public void AddSystemMessage(string content)
        {
            AddMessage(new ConversationMessage
            {
                Role = MessageRole.System,
                Content = content
            });
        }

        /// <summary>
        /// Add a user message
        /// </summary>
        public void AddUserMessage(string content)
        {
            AddMessage(new ConversationMessage
            {
                Role = MessageRole.User,
                Content = content
            });
        }

        /// <summary>
        /// Add an assistant message
        /// </summary>
        public void AddAssistantMessage(string content, IList<ToolCallRequest> toolCalls = null)
        {
            AddMessage(new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = content,
                ToolCalls = toolCalls
            });
        }

        /// <summary>
        /// Add a tool response message
        /// </summary>
        public void AddToolMessage(string toolCallId, string toolName, string content)
        {
            AddMessage(new ConversationMessage
            {
                Role = MessageRole.Tool,
                ToolCallId = toolCallId,
                ToolName = toolName,
                Content = content
            });
        }
    }
}
