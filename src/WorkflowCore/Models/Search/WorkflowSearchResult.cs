using System;
using System.Collections.Generic;

namespace WorkflowCore.Models.Search
{
    public class WorkflowSearchResult
    {
        public string Id { get; set; }

        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public string Reference { get; set; }

        public DateTime? NextExecutionUtc { get; set; }

        public WorkflowStatus Status { get; set; }

        public object Data { get; set; }
        
        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public ICollection<StepInfo> WaitingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> SleepingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> FailedSteps { get; set; } = new HashSet<StepInfo>();


    }

    public class WorkflowSearchResult<TData> : WorkflowSearchResult
    {
        public new TData Data { get; set; }
    }
    
}
