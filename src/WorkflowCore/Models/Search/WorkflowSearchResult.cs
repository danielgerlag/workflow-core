using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Models.Search
{
    public class WorkflowSearchResult<TData>
    {
        public string Id { get; set; }

        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public string Reference { get; set; }

        public DateTime? NextExecutionUtc { get; set; }

        public string Status { get; set; }

        public TData Data { get; set; }

        public IEnumerable<string> DataTokens { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public ICollection<StepInfo> WaitingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> SleepingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> FailedSteps { get; set; } = new HashSet<StepInfo>();

    }

    public class WorkflowSearchResult : WorkflowSearchResult<object>
    {
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

            if (workflow.Data is ISearchable)
                result.DataTokens = (workflow.Data as ISearchable).GetSearchTokens();

            foreach (var ep in workflow.ExecutionPointers)
            {
                if (ep.Status == PointerStatus.Sleeping)
                {
                    result.SleepingSteps.Add(new StepInfo()
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }

                if (ep.Status == PointerStatus.WaitingForEvent)
                {
                    result.WaitingSteps.Add(new StepInfo()
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }

                if (ep.Status == PointerStatus.Failed)
                {
                    result.FailedSteps.Add(new StepInfo()
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }
            }

            return result;
        }
    }
    
}
