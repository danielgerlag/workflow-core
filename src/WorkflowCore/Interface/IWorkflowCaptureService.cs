using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Interface
{
    public interface IWorkflowCaptureService
    {
        Task<PendingActivity> CaptureActivity(string activity, string workflowInstanceId, CancellationToken cancellationToken = default);
        
        Task<LifeCycleEvent> CaptureWorkflowCompletion(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}