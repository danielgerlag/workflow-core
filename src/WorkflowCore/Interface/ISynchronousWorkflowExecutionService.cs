using System.Threading.Tasks;
using WorkflowCore.Services;

namespace WorkflowCore.Interface
{
    public interface ISynchronousWorkflowExecutionService
    {
        Task<SynchronousWorkflowExecutionResult> StartWorkflowAndWait<TData>(string workflowId, int? version = null, TData data = null, string reference = null) where TData : class, new();
    }
}