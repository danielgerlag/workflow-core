using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {

        private readonly IServiceProvider _serviceProvider;
        private List<Tuple<string, int, WorkflowDefinition>> _registry = new List<Tuple<string, int, WorkflowDefinition>>();

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WorkflowDefinition GetDefinition(string workflowId, int version)
        {
            var entry = _registry.FirstOrDefault(x => x.Item1 == workflowId && x.Item2 == version);
            if (entry == null)
                return null;
            return entry.Item3;
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
                throw new Exception(String.Format("Workflow {0} version {1} is already registed", workflow.Id, workflow.Version));

            var builder = (_serviceProvider.GetService(typeof(IWorkflowBuilder)) as IWorkflowBuilder).UseData<object>();            
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(new Tuple<string, int, WorkflowDefinition>(workflow.Id, workflow.Version, def));
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
        {
            if (_registry.Any(x => x.Item1 == workflow.Id && x.Item2 == workflow.Version))
                throw new Exception(String.Format("Workflow {0} version {1} is already registed", workflow.Id, workflow.Version));

            var builder = (_serviceProvider.GetService(typeof(IWorkflowBuilder)) as IWorkflowBuilder).UseData<TData>();
            workflow.Build(builder);
            var def = builder.Build(workflow.Id, workflow.Version);
            _registry.Add(new Tuple<string, int, WorkflowDefinition>(workflow.Id, workflow.Version, def));
        }
    }
}
