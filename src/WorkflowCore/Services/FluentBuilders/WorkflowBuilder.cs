using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Services
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

        protected ICollection<IWorkflowBuilder> Branches { get; set; } = new List<IWorkflowBuilder>();

        protected WorkflowErrorHandling DefaultErrorBehavior = WorkflowErrorHandling.Retry;

        protected TimeSpan? DefaultErrorRetryInterval;

        public int LastStep => Steps.Max(x => x.Id);

        public IWorkflowBuilder<T> UseData<T>()
        {
            IWorkflowBuilder<T> result = new WorkflowBuilder<T>(Steps);
            return result;
        }

        public virtual WorkflowDefinition Build(string id, int version)
        {
            AttachExternalIds();
            return new WorkflowDefinition
            {
                Id = id,
                Version = version,
                Steps = new WorkflowStepCollection(Steps),
                DefaultErrorBehavior = DefaultErrorBehavior,
                DefaultErrorRetryInterval = DefaultErrorRetryInterval
            };
        }

        public void AddStep(WorkflowStep step)
        {
            step.Id = Steps.Count();
            Steps.Add(step);
        }

        private void AttachExternalIds()
        {
            foreach (var step in Steps)
            {
                foreach (var outcome in step.Outcomes.Where(x => !string.IsNullOrEmpty(x.ExternalNextStepId)))
                {
                    if (Steps.All(x => x.ExternalId != outcome.ExternalNextStepId))
                        throw new KeyNotFoundException($"Cannot find step id {outcome.ExternalNextStepId}");

                    outcome.NextStep = Steps.Single(x => x.ExternalId == outcome.ExternalNextStepId).Id;
                }
            }
        }

        public void AttachBranch(IWorkflowBuilder branch)
        {
            if (Branches.Contains(branch))
                return;

            foreach (var step in branch.Steps)
            {
                var oldId = step.Id;
                AddStep(step);
                foreach (var step2 in branch.Steps)
                {
                    foreach (var outcome in step2.Outcomes)
                    {
                        if (outcome.NextStep == oldId)
                            outcome.NextStep = step.Id;
                    }
                }
            }

            Branches.Add(branch);
        }

    }

    public class WorkflowBuilder<TData> : WorkflowBuilder, IWorkflowBuilder<TData>
    {

        public override WorkflowDefinition Build(string id, int version)
        {
            var result = base.Build(id, version);
            result.DataType = typeof(TData);
            return result;
        }

        public WorkflowBuilder(IEnumerable<WorkflowStep> steps)
        {
            this.Steps.AddRange(steps);
        }

        public IStepBuilder<TData, TStep> StartWith<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> step = new WorkflowStep<TStep>();
            var stepBuilder = new StepBuilder<TData, TStep>(this, step);

            if (stepSetup != null)
            {
                stepSetup.Invoke(stepBuilder);
            }

            step.Name = step.Name ?? typeof(TStep).Name;
            AddStep(step);
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> StartWith(Func<IStepExecutionContext, ExecutionResult> body)
        {
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(this, newStep);
            AddStep(newStep);
            return stepBuilder;
        }

        public IStepBuilder<TData, ActionStepBody> StartWith(Action<IStepExecutionContext> body)
        {
            var newStep = new WorkflowStep<ActionStepBody>();
            AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, ActionStepBody>(this, newStep);
            stepBuilder.Input(x => x.Body, x => body);
            return stepBuilder;
        }

        public IEnumerable<WorkflowStep> GetUpstreamSteps(int id)
        {
            return Steps.Where(x => x.Outcomes.Any(y => y.NextStep == id)).ToList();
        }

        public IWorkflowBuilder<TData> UseDefaultErrorBehavior(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null)
        {
            DefaultErrorBehavior = behavior;
            DefaultErrorRetryInterval = retryInterval;
            return this;
        }

        public IWorkflowBuilder<TData> CreateBranch()
        {
            var result = new WorkflowBuilder<TData>(new List<WorkflowStep>());
            return result;
        }
                
    }
        
}
