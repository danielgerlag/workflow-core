using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Interface for tools that can be invoked by the LLM
    /// </summary>
    public interface IAgentTool
    {
        /// <summary>
        /// Name of the tool (must be unique)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the tool does (used by the LLM to decide when to use it)
        /// </summary>
        string Description { get; }

        /// <summary>
        /// JSON schema for the tool's parameters
        /// </summary>
        string ParametersSchema { get; }

        /// <summary>
        /// Execute the tool with the given arguments
        /// </summary>
        /// <param name="arguments">JSON string containing the tool arguments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tool execution result</returns>
        Task<ToolResult> ExecuteAsync(string toolCallId, string arguments, CancellationToken cancellationToken = default);
    }
}
