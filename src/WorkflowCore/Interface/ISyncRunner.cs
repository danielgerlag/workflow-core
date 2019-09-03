using System;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ISyncRunner
    {
        Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, TimeSpan timeOut)
            where TData : new();
    }
}