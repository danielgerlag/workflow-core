using System;

namespace WorkflowCore.Models.LifeCycleEvents
{
    public class WorkflowTerminated : LifeCycleEvent
    {
        public SerializableException Exception { get; set; }
    }
}
