using System;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ISyncWorkflowRunner
    {
        Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, TimeSpan timeOut, bool persistSate = true)
            where TData : new();
    }
}