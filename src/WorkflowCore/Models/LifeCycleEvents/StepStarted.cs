using System;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class StepStarted : LifeCycleEvent
    {
        public string ExecutionPointerId { get; set; }

        public int StepId { get; set; }
    }
}
