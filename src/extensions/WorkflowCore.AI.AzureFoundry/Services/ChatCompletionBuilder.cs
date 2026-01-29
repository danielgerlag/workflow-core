using System;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Builder for ChatCompletion steps
    /// </summary>
    public class ChatCompletionBuilder<TData> : StepBuilder<TData, ChatCompletion>, IChatCompletionBuilder<TData>
    {
        public ChatCompletionBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<ChatCompletion> step)
            : base(workflowBuilder, step)
        {
        }

        public IChatCompletionBuilder<TData> SystemPrompt(string prompt)
        {
            Input(s => s.SystemPrompt, d => prompt);
            return this;
        }

        public IChatCompletionBuilder<TData> SystemPrompt(Expression<Func<TData, string>> expression)
        {
            Input(s => s.SystemPrompt, expression);
            return this;
        }

        public IChatCompletionBuilder<TData> UserMessage(string message)
        {
            Input(s => s.UserMessage, d => message);
            return this;
        }

        public IChatCompletionBuilder<TData> UserMessage(Expression<Func<TData, string>> expression)
        {
            Input(s => s.UserMessage, expression);
            return this;
        }

        public IChatCompletionBuilder<TData> Model(string model)
        {
            Input(s => s.Model, d => model);
            return this;
        }

        public IChatCompletionBuilder<TData> Temperature(float temperature)
        {
            Input(s => s.Temperature, d => temperature);
            return this;
        }

        public IChatCompletionBuilder<TData> MaxTokens(int maxTokens)
        {
            Input(s => s.MaxTokens, d => maxTokens);
            return this;
        }

        public IChatCompletionBuilder<TData> WithHistory(bool include = true)
        {
            Input(s => s.IncludeHistory, d => include);
            return this;
        }

        public IChatCompletionBuilder<TData> ThreadId(Expression<Func<TData, string>> expression)
        {
            Input(s => s.ThreadId, expression);
            return this;
        }

        public IChatCompletionBuilder<TData> OutputTo(Expression<Func<TData, string>> expression)
        {
            Output(expression, s => s.Response);
            return this;
        }

        public IChatCompletionBuilder<TData> OutputTokensTo(Expression<Func<TData, int>> expression)
        {
            Output(expression, s => s.TotalTokens);
            return this;
        }
    }
}
