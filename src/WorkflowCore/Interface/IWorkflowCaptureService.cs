using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IWorkflowCaptureService
    {
        Task CaptureWorkflowStop(string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}