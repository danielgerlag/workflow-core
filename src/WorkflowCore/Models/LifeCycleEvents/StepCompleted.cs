using System;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class StepCompleted : LifeCycleEvent
    {
        public string ExecutionPointerId { get; set; }

        public int StepId { get; set; }
    }
}
