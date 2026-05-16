using System;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Builder interface for configuring ChatCompletion steps
    /// </summary>
    public interface IChatCompletionBuilder<TData> : IStepBuilder<TData, ChatCompletion>
    {
        /// <summary>
        /// Set the system prompt
        /// </summary>
        IChatCompletionBuilder<TData> SystemPrompt(string prompt);

        /// <summary>
        /// Set the system prompt from workflow data
        /// </summary>
        IChatCompletionBuilder<TData> SystemPrompt(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the user message
        /// </summary>
        IChatCompletionBuilder<TData> UserMessage(string message);

        /// <summary>
        /// Set the user message from workflow data
        /// </summary>
        IChatCompletionBuilder<TData> UserMessage(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the model to use
        /// </summary>
        IChatCompletionBuilder<TData> Model(string model);

        /// <summary>
        /// Set the temperature
        /// </summary>
        IChatCompletionBuilder<TData> Temperature(float temperature);

        /// <summary>
        /// Set the max tokens
        /// </summary>
        IChatCompletionBuilder<TData> MaxTokens(int maxTokens);

        /// <summary>
        /// Include conversation history
        /// </summary>
        IChatCompletionBuilder<TData> WithHistory(bool include = true);

        /// <summary>
        /// Set the thread ID for conversation history
        /// </summary>
        IChatCompletionBuilder<TData> ThreadId(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Output the response to workflow data
        /// </summary>
        IChatCompletionBuilder<TData> OutputTo(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Output token usage to workflow data
        /// </summary>
        IChatCompletionBuilder<TData> OutputTokensTo(Expression<Func<TData, int>> expression);
    }
}
