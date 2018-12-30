using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class WorkflowError : LifeCycleEvent
    {
        public string Message { get; set; }

        public string ExecutionPointerId { get; set; }

        public int StepId { get; set; }
    }
}
