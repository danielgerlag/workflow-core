using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Services;

namespace WorkflowCore.Interface
{
    public interface ISynchronousWorkflowExecutionService
    {
        Task<SynchronousWorkflowExecutionResult> StartWorkflowAsync<TData>(string workflowId, int? version = null, TData data = null, string reference = null) where TData : class, new();

        Task<object> RunWorkflowUntilActivityAsync<TData>(string workflowId, string activity, int? version = null, TData data = null, string reference = null, CancellationToken cancellationToken = default) where TData : class, new();

    }
}