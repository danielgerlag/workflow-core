using System;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class WorkflowTerminated : LifeCycleEvent
    {
        public Exception Exception { get; set; }
    }
}
