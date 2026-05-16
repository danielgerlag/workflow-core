using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Builder for AgentLoop steps
    /// </summary>
    public class AgentLoopBuilder<TData> : StepBuilder<TData, AgentLoop>, IAgentLoopBuilder<TData>
    {
        private readonly List<string> _toolNames = new List<string>();

        public AgentLoopBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<AgentLoop> step)
            : base(workflowBuilder, step)
        {
        }

        public IAgentLoopBuilder<TData> SystemPrompt(string prompt)
        {
            Input(s => s.SystemPrompt, d => prompt);
            return this;
        }

        public IAgentLoopBuilder<TData> SystemPrompt(Expression<Func<TData, string>> expression)
        {
            Input(s => s.SystemPrompt, expression);
            return this;
        }

        public IAgentLoopBuilder<TData> Message(string message)
        {
            Input(s => s.UserMessage, d => message);
            return this;
        }

        public IAgentLoopBuilder<TData> Message(Expression<Func<TData, string>> expression)
        {
            Input(s => s.UserMessage, expression);
            return this;
        }

        public IAgentLoopBuilder<TData> Model(string model)
        {
            Input(s => s.Model, d => model);
            return this;
        }

        public IAgentLoopBuilder<TData> MaxIterations(int maxIterations)
        {
            Input(s => s.MaxIterations, d => maxIterations);
            return this;
        }

        public IAgentLoopBuilder<TData> WithTool<TTool>() where TTool : IAgentTool
        {
            // Tool name will be resolved at runtime
            _toolNames.Add(typeof(TTool).Name);
            Input(s => s.AvailableTools, d => _toolNames);
            return this;
        }

        public IAgentLoopBuilder<TData> WithTool(string toolName)
        {
            _toolNames.Add(toolName);
            Input(s => s.AvailableTools, d => _toolNames);
            return this;
        }

        public IAgentLoopBuilder<TData> AutoExecuteTools(bool auto = true)
        {
            Input(s => s.AutomaticMode, d => auto);
            return this;
        }

        public IAgentLoopBuilder<TData> OutputTo(Expression<Func<TData, string>> expression)
        {
            Output(expression, s => s.Response);
            return this;
        }
    }
}
