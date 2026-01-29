using System;
using System.Collections.Generic;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Defines a tool that can be invoked by the LLM
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>
        /// Name of the tool (must be unique)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the tool does (used by the LLM)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// JSON schema for the tool's parameters
        /// </summary>
        public string ParametersSchema { get; set; }

        /// <summary>
        /// Whether the tool requires confirmation before execution
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Type that implements the tool execution
        /// </summary>
        public Type ImplementationType { get; set; }
    }
}
