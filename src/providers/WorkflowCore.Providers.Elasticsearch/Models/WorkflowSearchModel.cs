using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.Search;

namespace WorkflowCore.Providers.Elasticsearch.Models
{
    public class WorkflowSearchModel
    {
        public string Id { get; set; }

        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public string Reference { get; set; }

        public DateTime? NextExecutionUtc { get; set; }

        public string Status { get; set; }

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public IEnumerable<string> DataTokens { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public ICollection<StepInfo> WaitingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> SleepingSteps { get; set; } = new HashSet<StepInfo>();

        public ICollection<StepInfo> FailedSteps { get; set; } = new HashSet<StepInfo>();

        public WorkflowSearchResult ToSearchResult()
        {
            var result = new WorkflowSearchResult
            {
                Id = Id,
                CompleteTime = CompleteTime,
                CreateTime = CreateTime,
                Description = Description,
                NextExecutionUtc = NextExecutionUtc,
                Reference = Reference,
                Status = (WorkflowStatus) Enum.Parse(typeof(WorkflowStatus), Status, true),
                Version = Version,
                WorkflowDefinitionId = WorkflowDefinitionId,
                FailedSteps = FailedSteps,
                SleepingSteps = SleepingSteps,
                WaitingSteps = WaitingSteps
            };

            if (Data.Count > 0)
                result.Data = Data.First().Value;

            return result;
        }

        public static WorkflowSearchModel FromWorkflowInstance(WorkflowInstance workflow)
        {
            var result = new WorkflowSearchModel();

            result.Id = workflow.Id;
            result.WorkflowDefinitionId = workflow.WorkflowDefinitionId;
            result.Description = workflow.Description;
            result.Reference = workflow.Reference;

            if (workflow.Data != null)
                result.Data.Add(workflow.Data.GetType().FullName, workflow.Data);

            result.CompleteTime = workflow.CompleteTime;
            result.CreateTime = workflow.CreateTime;
            result.Version = workflow.Version;
            result.Status = workflow.Status.ToString();

            if (workflow.NextExecution.HasValue)
                result.NextExecutionUtc = new DateTime(workflow.NextExecution.Value);

            if (workflow.Data is ISearchable)
                result.DataTokens = (workflow.Data as ISearchable).GetSearchTokens();

            foreach (var ep in workflow.ExecutionPointers)
            {
                if (ep.Status == PointerStatus.Sleeping)
                {
                    result.SleepingSteps.Add(new StepInfo
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }

                if (ep.Status == PointerStatus.WaitingForEvent)
                {
                    result.WaitingSteps.Add(new StepInfo
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }

                if (ep.Status == PointerStatus.Failed)
                {
                    result.FailedSteps.Add(new StepInfo
                    {
                        StepId = ep.StepId,
                        Name = ep.StepName
                    });
                }
            }

            return result;
        }
        
    }

    public class TypedWorkflowSearchModel<T> : WorkflowSearchModel
    {
        public new Dictionary<string, T> Data { get; set; }
    }
}
