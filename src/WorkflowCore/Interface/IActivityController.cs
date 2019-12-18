using System;
using System.Collections.Generic;
using System.Text;
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
        Task<PendingActivity> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null);
        Task ReleaseActivityToken(string token);
        Task SubmitActivitySuccess(string token, object result);
        Task SubmitActivityFailure(string token, object result);

    }
}
