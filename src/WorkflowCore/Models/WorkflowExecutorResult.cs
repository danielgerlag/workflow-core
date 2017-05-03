using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models
{
    public class WorkflowExecutorResult
    {
        public List<EventSubscription> AddSubscriptions { get; set; } = new List<EventSubscription>();
    }
}
