using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public int InitialStep { get; set; }

        public List<WorkflowStep> Steps { get; set; }

        public Type DataType { get; set; }

        public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

        public TimeSpan? DefaultErrorRetryInterval { get; set; }

    }

    public enum WorkflowErrorHandling { Retry = 0, Suspend = 1, Terminate = 2 }
}
