using System;
using System.Linq;

namespace WorkflowCore.Models
{
    public class WorkflowInstance
    {
        public string Id { get; set; }
                
        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public string Reference { get; set; }

        public ExecutionPointerCollection ExecutionPointers { get; set; } = new ExecutionPointerCollection();

        public long? NextExecution { get; set; }

        public WorkflowStatus Status { get; set; }

        public object Data { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public bool IsBranchComplete(string parentId)
        {
            return ExecutionPointers
                .FindByScope(parentId)
                .All(x => x.EndTime != null);
        }
    }

    public enum WorkflowStatus
    { 
        Runnable = 0,
        Suspended = 1,
        Complete = 2,
        Terminated = 3,
    }
}
