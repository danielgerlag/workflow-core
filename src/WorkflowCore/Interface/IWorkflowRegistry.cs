using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(IWorkflow workflow);
        Task RegisterWorkflowAsync(IWorkflow workflow);
        void RegisterWorkflow(WorkflowDefinition definition);
        Task RegisterWorkflowAsync(WorkflowDefinition definition);
        void RegisterWorkflow<TData>(IWorkflow<TData> workflow) where TData : new();
        Task RegisterWorkflowAsync<TData>(IWorkflow<TData> workflow) where TData : new();
        WorkflowDefinition GetDefinition(string workflowId, int? version = null);
        Task<WorkflowDefinition> GetDefinitionAsync(string workflowId, int? version = null);
        bool IsRegistered(string workflowId, int version);
        Task<bool> IsRegisteredAsync(string workflowId, int version);
        void DeregisterWorkflow(string workflowId, int version);
        Task DeregisterWorkflowAsync(string workflowId, int version);
        IEnumerable<WorkflowDefinition> GetAllDefinitions();
    }
}
