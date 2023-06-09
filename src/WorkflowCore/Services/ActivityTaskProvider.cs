using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class ActivityTaskProvider : IActivityTaskProvider
    {
        private readonly IWorkflowHost _host;
        
        public ActivityTaskProvider(IWorkflowHost host)
        {
            _host = host;
        }
        
        public async Task WaitForActivityCreation(string activity, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await _host.GetPendingActivity(activity, workflowInstanceId, cancellationToken);
        }
    }
}