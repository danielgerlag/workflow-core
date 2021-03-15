using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BlockingCollection<(string workflowId, int version, WorkflowDefinition definition)> _registry = new BlockingCollection<(string, int, WorkflowDefinition)>();

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WorkflowDefinition GetDefinition(string workflowId, int? version = null)
        {
            (string workflowId, int version, WorkflowDefinition definition) workflowEntry;
            if (version.HasValue)
            {
                workflowEntry = _registry.FirstOrDefault(x => x.workflowId == workflowId && x.version == version.Value);
            }
            else
            {
                workflowEntry = _registry.Where(x => x.workflowId == workflowId).OrderByDescending(x => x.version)
                    .FirstOrDefault();
            }

            return workflowEntry != default ? workflowEntry.definition : default;
        }

        public void DeregisterWorkflow(string workflowId, int version)
        {
            var definition = _registry.FirstOrDefault(x => x.workflowId == workflowId && x.version == version);
            if (definition != default)
            {
                _registry.TryTake(out definition);
            }
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            if (_registry.Any(x => x.workflowId == workflow.Id && x.version == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registered");
            }

            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<object>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add((workflow.Id, workflow.Version, def));
        }

        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            if (_registry.Any(x => x.workflowId == definition.Id && x.version == definition.Version))
            {
                throw new InvalidOperationException($"Workflow {definition.Id} version {definition.Version} is already registered");
            }

            _registry.Add((definition.Id, definition.Version, definition));
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
            where TData : new()
        {
            if (_registry.Any(x => x.workflowId == workflow.Id && x.version == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registered");
            }

            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<TData>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add((workflow.Id, workflow.Version, def));
        }

        public bool IsRegistered(string workflowId, int version)
        {
            var definition = _registry.FirstOrDefault(x => x.workflowId == workflowId && x.version == version);
            return definition != default;
        }

        public IEnumerable<WorkflowDefinition> GetAllDefinitions()
        {
            return _registry.Select(i => i.definition);
        }
    }
}