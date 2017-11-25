using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Tuple<string, int, WorkflowDefinition>> _registry = new List<Tuple<string, int, WorkflowDefinition>>();

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WorkflowDefinition GetDefinition(string workflowId, int? version = null)
        {
            if (version.HasValue)
            {
                var entry = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == version.Value);
                if (entry == null)
                {
                    return null;
                }

                // TODO: What in the heack does Item3 mean?
                return entry.Item3;
            }
            else
            {
                int maxVersion = _registry.Where(x => x.Item1 == workflowId).Max(x => x.Item2);
                var entry = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == maxVersion);
                if (entry == null)
                {
                    return null;
                }

                return entry.Item3;
            }
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registered");
            }

            var builder = (_serviceProvider.GetService(typeof(IWorkflowBuilder)) as IWorkflowBuilder).UseData<object>();            
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(new Tuple<string, int, WorkflowDefinition>(workflow.Id, workflow.Version, def));
        }

        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            if (_registry.Any(x => x.Item1 == definition.Id && x.Item2 == definition.Version))
            {
                throw new InvalidOperationException($"Workflow {definition.Id} version {definition.Version} is already registered");
            }

            _registry.Add(new Tuple<string, int, WorkflowDefinition>(definition.Id, definition.Version, definition));
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
            where TData : new()
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
            {
                throw new InvalidOperationException($"Workflow {workflow.Id} version {workflow.Version} is already registed");
            }

            var builder = (_serviceProvider.GetService(typeof(IWorkflowBuilder)) as IWorkflowBuilder).UseData<TData>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(new Tuple<string, int, WorkflowDefinition>(workflow.Id, workflow.Version, def));
        }
    }
}
