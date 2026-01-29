using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Registry for managing agent tools
    /// </summary>
    public class ToolRegistry : IToolRegistry
    {
        private readonly ConcurrentDictionary<string, IAgentTool> _tools = 
            new ConcurrentDictionary<string, IAgentTool>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Type> _toolTypes = 
            new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private readonly IServiceProvider _serviceProvider;

        public ToolRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Register(IAgentTool tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (string.IsNullOrEmpty(tool.Name))
                throw new ArgumentException("Tool name cannot be null or empty", nameof(tool));

            _tools[tool.Name] = tool;
        }

        public void Register<T>() where T : IAgentTool
        {
            var tool = _serviceProvider.GetRequiredService<T>();
            Register(tool);
            _toolTypes[tool.Name] = typeof(T);
        }

        public IAgentTool GetTool(string name)
        {
            if (_tools.TryGetValue(name, out var tool))
                return tool;

            if (_toolTypes.TryGetValue(name, out var type))
            {
                tool = (IAgentTool)_serviceProvider.GetRequiredService(type);
                _tools[name] = tool;
                return tool;
            }

            return null;
        }

        public IEnumerable<IAgentTool> GetAllTools()
        {
            return _tools.Values.ToList();
        }

        public IEnumerable<ToolDefinition> GetToolDefinitions()
        {
            return _tools.Values.Select(t => new ToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                ParametersSchema = t.ParametersSchema,
                ImplementationType = t.GetType()
            }).ToList();
        }

        public bool HasTool(string name)
        {
            return _tools.ContainsKey(name) || _toolTypes.ContainsKey(name);
        }
    }
}
