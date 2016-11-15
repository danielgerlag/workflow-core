using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(IWorkflow workflow);
        void RegisterWorkflow<TData>(IWorkflow<TData> workflow);
        WorkflowDefinition GetDefinition(string workflowId, int version);
             
    }
}
