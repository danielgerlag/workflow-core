using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BlockingCollection<Tuple<string, int, WorkflowDefinition>> _registry = new BlockingCollection<Tuple<string, int, WorkflowDefinition>>();

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WorkflowDefinition GetDefinition(string workflowId, int? version = null)
        {
            if (version.HasValue)
            {
                var entry = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == version.Value);
                // TODO: What in the heck does Item3 mean?
                return entry?.Item3;
            }
            else
            {
                var entry = _registry.Where(x => x.Item1 == workflowId).OrderByDescending(x => x.Item2)
                                     .FirstOrDefault();
                return entry?.Item3;
            }
        }

        public void DeregisterWorkflow(string workflowId, int version)
        {
            var definition = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == version);
            if (definition != null)
            {
                _registry.TryTake(out definition);
            }
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registered");
            }

            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<object>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(Tuple.Create(workflow.Id, workflow.Version, def));
        }

        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            if (_registry.Any(x => x.Item1 == definition.Id && x.Item2 == definition.Version))
            {
                throw new InvalidOperationException($"Workflow {definition.Id} version {definition.Version} is already registered");
            }

            _registry.Add(Tuple.Create(definition.Id, definition.Version, definition));
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
            where TData : new()
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registered");
            }

            var builder = _serviceProvider.GetService<IWorkflowBuilder>().UseData<TData>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(Tuple.Create(workflow.Id, workflow.Version, def));
        }

        public bool IsRegistered(string workflowId, int version)
        {
            var definition = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == version);
            return (definition != null);
        }

        public IEnumerable<WorkflowDefinition> GetAllDefinitions()
        {
            return _registry.Select(i => i.Item3);
        }

        public Task RegisterWorkflowAsync(IWorkflow workflow)
        {
            RegisterWorkflow(workflow);
            return Task.CompletedTask;
        }

        public Task RegisterWorkflowAsync(WorkflowDefinition definition)
        {
            RegisterWorkflow(definition);
            return Task.CompletedTask;
        }

        public Task RegisterWorkflowAsync<TData>(IWorkflow<TData> workflow) where TData : new()
        {
            RegisterWorkflow(workflow);
            return Task.CompletedTask;
        }

        public Task<WorkflowDefinition> GetDefinitionAsync(string workflowId, int? version = null)
        {
            var definition = GetDefinition(workflowId, version);
            return Task.FromResult(definition);
        }

        public Task<bool> IsRegisteredAsync(string workflowId, int version)
        {
            var isRegistered = IsRegistered(workflowId, version);
            return Task.FromResult(isRegistered);
        }

        public Task DeregisterWorkflowAsync(string workflowId, int version)
        {
            DeregisterWorkflow(workflowId, version);
            return Task.CompletedTask;
        }
    }
}
