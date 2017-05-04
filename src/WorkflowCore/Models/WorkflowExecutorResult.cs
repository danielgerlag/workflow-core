using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models
{
    public class WorkflowExecutorResult
    {
        public List<EventSubscription> Subscriptions { get; set; } = new List<EventSubscription>();
        public List<ExecutionError> Errors { get; set; } = new List<ExecutionError>();
    }
}
