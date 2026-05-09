using System;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class WorkflowForked : LifeCycleEvent
    {
        public string SourceWorkflowInstanceId { get; set; }
    }
}
