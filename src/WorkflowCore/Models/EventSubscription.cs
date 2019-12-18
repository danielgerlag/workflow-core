using System;

namespace WorkflowCore.Models
{
    public class EventSubscription
    {
        public string Id { get; set; }

        public string WorkflowId { get; set; }

        public int StepId { get; set; }

        public string ExecutionPointerId { get; set; }

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public DateTime SubscribeAsOf { get; set; }

        public object SubscriptionData { get; set; }
        
        public string ExternalToken { get; set; }
        
        public string ExternalWorkerId { get; set; }
        
        public DateTime? ExternalTokenExpiry { get; set; }
    }
}
