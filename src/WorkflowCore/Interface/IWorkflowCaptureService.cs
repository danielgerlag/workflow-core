using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IWorkflowCaptureService
    {
        Task<PendingActivity> CaptureActivity(string activity, string workflowInstanceId, CancellationToken cancellationToken = default);
        
        Task CaptureWorkflowExceptions(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}