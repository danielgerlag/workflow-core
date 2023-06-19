using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowCaptureService
    {
        Task<PendingActivity> CaptureActivity(string workflowInstanceId, CancellationToken cancellationToken = default);
        
        Task<WorkflowInstance> CaptureWorkflowCompletion(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}