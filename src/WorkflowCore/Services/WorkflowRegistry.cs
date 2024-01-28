using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _registry = new ConcurrentDictionary<string, WorkflowDefinition>();
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _latestVersion = new ConcurrentDictionary<string, WorkflowDefinition>();

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WorkflowDefinition GetDefinition(string workflowId, int? version = null)
        {
            if (version.HasValue)
            {
                if (!_registry.ContainsKey($"{workflowId}-{version}"))
                    return default;
                return _registry[$"{workflowId}-{version}"];
            }
            else
            {
                if (!_latestVersion.ContainsKey(workflowId))
                    return default;
                return _latestVersion[workflowId];
            }
        }

        public void DeregisterWorkflow(string workflowId, int version)
        {
            if (!_registry.ContainsKey($"{workflowId}-{version}"))
                return;

            lock (_registry)
            {
                _registry.TryRemove($"{workflowId}-{version}", out var _);
                if (_latestVersion[workflowId].Version == version)
                {
                    _latestVersion.TryRemove(workflowId, out var _);

                    var latest = _registry.Values.Where(x => x.Id == workflowId).OrderByDescending(x => x.Version).FirstOrDefault();
                    if (latest != default)
                        _latestVersion[workflowId] = latest;
                }
            }
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<object>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            RegisterWorkflow(def);
        }

        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            if (_registry.ContainsKey($"{definition.Id}-{definition.Version}"))
            {
                throw new InvalidOperationException($"Workflow {definition.Id} version {definition.Version} is already registered");
            }

            lock (_registry)
            {
                _registry[$"{definition.Id}-{definition.Version}"] = definition;
                if (!_latestVersion.ContainsKey(definition.Id))
                {
                    _latestVersion[definition.Id] = definition;
                    return;
                }

                if (_latestVersion[definition.Id].Version <= definition.Version)
                    _latestVersion[definition.Id] = definition;
            }
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
            where TData : new()
        {
            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<TData>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            RegisterWorkflow(def);
        }

        public bool IsRegistered(string workflowId, int version)
        {
            return _registry.ContainsKey($"{workflowId}-{version}");
        }

        public IEnumerable<WorkflowDefinition> GetAllDefinitions()
        {
            return _registry.Values;
        }
    }
}