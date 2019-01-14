using System;
using System.Collections.Generic;
using System.Text;

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

        public string Status { get; set; }

        public object Data { get; set; }

        public string DataTokens { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public ICollection<StepInfo> WaitingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> SleepingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> FailedSteps { get; set; } = new HashSet<StepInfo>();

        public static WorkflowSearchResult FromWorkflowInstance(WorkflowInstance workflow)
        {
            var result = new WorkflowSearchResult
            {
                Id = workflow.Id,
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Description = workflow.Description,
                Reference = workflow.Reference,
                Data = workflow.Data,
                CompleteTime = workflow.CompleteTime,
                CreateTime = workflow.CreateTime,
                Version = workflow.Version,
                Status = workflow.Status.ToString()                
            };

            if (workflow.NextExecution.HasValue)
                result.NextExecutionUtc = new DateTime(workflow.NextExecution.Value);
            

            return result;
        }
    }
}
