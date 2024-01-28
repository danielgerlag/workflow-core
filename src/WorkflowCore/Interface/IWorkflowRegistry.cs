using System.Collections.Generic;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(IWorkflow workflow);
        void RegisterWorkflow(WorkflowDefinition definition);
        void RegisterWorkflow<TData>(IWorkflow<TData> workflow) where TData : new();
        WorkflowDefinition GetDefinition(string workflowId, int? version = null);
        bool IsRegistered(string workflowId, int version);
        void DeregisterWorkflow(string workflowId, int version);
        IEnumerable<WorkflowDefinition> GetAllDefinitions();
    }
}
