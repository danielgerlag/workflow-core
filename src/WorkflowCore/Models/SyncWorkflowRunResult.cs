using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class SyncWorkflowRunResult
    {
        public Task CompletionTask { get; set; }
        public string InstanceId { get; set; }
    }
}