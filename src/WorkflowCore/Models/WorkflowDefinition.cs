using System;
using System.Collections.Generic;


namespace WorkflowCore.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public WorkflowStepCollection Steps { get; set; } = new WorkflowStepCollection();

        public Type DataType { get; set; }

        public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

        public Type OnPostMiddlewareError { get; set; }

        public TimeSpan? DefaultErrorRetryInterval { get; set; }

    }

    public enum WorkflowErrorHandling
    {
        Retry = 0,
        Suspend = 1,
        Terminate = 2,
        Compensate = 3
    }
}
