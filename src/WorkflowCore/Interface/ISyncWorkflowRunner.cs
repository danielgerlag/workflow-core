using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ISyncWorkflowRunner
    {
        Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, TimeSpan timeOut, bool persistState = true)
            where TData : new();

        Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, CancellationToken token, bool persistState = true)
            where TData : new();

        Task<WorkflowInstance> ResumeWorkflowSync(string workflowId, TimeSpan timeOut, bool persistState = true);

        Task<WorkflowInstance> ResumeWorkflowSync(string workflowId, CancellationToken token, bool persistState = true);
    }
}