using System;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Builder interface for configuring AgentLoop steps
    /// </summary>
    public interface IAgentLoopBuilder<TData> : IStepBuilder<TData, AgentLoop>
    {
        /// <summary>
        /// Set the system prompt
        /// </summary>
        IAgentLoopBuilder<TData> SystemPrompt(string prompt);

        /// <summary>
        /// Set the system prompt from workflow data
        /// </summary>
        IAgentLoopBuilder<TData> SystemPrompt(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the user message
        /// </summary>
        IAgentLoopBuilder<TData> Message(string message);

        /// <summary>
        /// Set the user message from workflow data
        /// </summary>
        IAgentLoopBuilder<TData> Message(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the model to use
        /// </summary>
        IAgentLoopBuilder<TData> Model(string model);

        /// <summary>
        /// Set maximum iterations
        /// </summary>
        IAgentLoopBuilder<TData> MaxIterations(int maxIterations);

        /// <summary>
        /// Add a tool by type
        /// </summary>
        IAgentLoopBuilder<TData> WithTool<TTool>() where TTool : IAgentTool;

        /// <summary>
        /// Add a tool by name
        /// </summary>
        IAgentLoopBuilder<TData> WithTool(string toolName);

        /// <summary>
        /// Enable/disable automatic tool execution
        /// </summary>
        IAgentLoopBuilder<TData> AutoExecuteTools(bool auto = true);

        /// <summary>
        /// Output the response to workflow data
        /// </summary>
        IAgentLoopBuilder<TData> OutputTo(Expression<Func<TData, string>> expression);
    }
}
