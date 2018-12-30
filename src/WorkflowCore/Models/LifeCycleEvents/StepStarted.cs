using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class StepStarted : LifeCycleEvent
    {
        public string ExecutionPointerId { get; set; }

        public int StepId { get; set; }
    }
}
