using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public class PendingActivity
    {
        public string Token { get; set; }
        public string ActivityName { get; set; }
        public object Parameters { get; set; }
        public DateTime TokenExpiry { get; set; }
        
    }
    
    public interface IActivityController
    {
        Task<PendingActivity> GetPendingActivity(string activityName, string workerId, CancellationToken cancellationToken = default);
        Task<PendingActivity> GetPendingActivity(string activityName, string workerId, string workflowId, CancellationToken cancellationToken = default);
        Task ReleaseActivityToken(string token);
        Task SubmitActivitySuccess(string token, object result);
        Task SubmitActivityFailure(string token, object result);

    }
}
