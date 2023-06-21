using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IWorkflowCaptureService
    {
        Task CaptureWorkflowStop(string workflowId, string activityToExclude = "", CancellationToken cancellationToken = default);
    }
}