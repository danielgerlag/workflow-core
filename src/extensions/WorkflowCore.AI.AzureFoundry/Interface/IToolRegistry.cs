using System.Collections.Generic;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Registry for agent tools
    /// </summary>
    public interface IToolRegistry
    {
        /// <summary>
        /// Register a tool
        /// </summary>
        void Register(IAgentTool tool);

        /// <summary>
        /// Register a tool by type
        /// </summary>
        void Register<T>() where T : IAgentTool;

        /// <summary>
        /// Get a tool by name
        /// </summary>
        IAgentTool GetTool(string name);

        /// <summary>
        /// Get all registered tools
        /// </summary>
        IEnumerable<IAgentTool> GetAllTools();

        /// <summary>
        /// Get tool definitions for all registered tools
        /// </summary>
        IEnumerable<ToolDefinition> GetToolDefinitions();

        /// <summary>
        /// Check if a tool is registered
        /// </summary>
        bool HasTool(string name);
    }
}
