using System;
using System.Collections.Generic;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Result from executing a tool
    /// </summary>
    public class ToolResult
    {
        /// <summary>
        /// Whether the tool executed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Result data from the tool (serialized as string for LLM consumption)
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Error message if the tool failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The tool call ID this result corresponds to
        /// </summary>
        public string ToolCallId { get; set; }

        /// <summary>
        /// Name of the tool that was executed
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Execution duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static ToolResult Succeeded(string toolCallId, string toolName, string result)
        {
            return new ToolResult
            {
                Success = true,
                ToolCallId = toolCallId,
                ToolName = toolName,
                Result = result
            };
        }

        /// <summary>
        /// Create a failed result
        /// </summary>
        public static ToolResult Failed(string toolCallId, string toolName, string error)
        {
            return new ToolResult
            {
                Success = false,
                ToolCallId = toolCallId,
                ToolName = toolName,
                Error = error,
                Result = $"Error: {error}"
            };
        }
    }
}
