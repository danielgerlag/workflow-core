using System;
using System.Collections.Generic;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Represents a single message in a conversation thread
    /// </summary>
    public class ConversationMessage
    {
        /// <summary>
        /// Unique identifier for this message
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Role of the message sender (system, user, assistant, tool)
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// Text content of the message
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Name of the tool that produced this message (for tool role)
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Tool call ID this message is responding to (for tool role)
        /// </summary>
        public string ToolCallId { get; set; }

        /// <summary>
        /// Tool calls requested by the assistant
        /// </summary>
        public IList<ToolCallRequest> ToolCalls { get; set; }

        /// <summary>
        /// When the message was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for the message
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Token count for this message (if available)
        /// </summary>
        public int? TokenCount { get; set; }
    }

    /// <summary>
    /// Role of a conversation message
    /// </summary>
    public enum MessageRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    /// <summary>
    /// Represents a tool call request from the LLM
    /// </summary>
    public class ToolCallRequest
    {
        /// <summary>
        /// Unique identifier for this tool call
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the tool to invoke
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// JSON arguments for the tool
        /// </summary>
        public string Arguments { get; set; }
    }
}
