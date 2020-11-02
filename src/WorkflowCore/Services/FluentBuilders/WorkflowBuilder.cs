using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

            var branchStart = LastStep + branch.LastStep + 1;

            foreach (var step in branch.Steps)
            {
                var oldId = step.Id;
                step.Id = oldId + branchStart;
                foreach (var step2 in branch.Steps)
                {
                    foreach (var outcome in step2.Outcomes)
                    {
                        if (outcome.NextStep == oldId)
                            outcome.NextStep = step.Id;
                    }

                    for (var i = 0; i < step2.Children.Count; i++)
                    {
                        if (step2.Children[i] == oldId)
                            step2.Children[i] = step.Id;
                    }
                }
            }

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

                    for (var i = 0; i < step2.Children.Count; i++)
                    {
                        if (step2.Children[i] == oldId)
                            step2.Children[i] = step.Id;
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

        public IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody
        {
            return Start().Then(stepSetup);
        }

        public IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> newStep) where TStep : IStepBody
        {
            return Start().Then(newStep);
        }

        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {
            return Start().Then(body);
        }

        public IStepBuilder<TData, ActionStepBody> Then(Action<IStepExecutionContext> body)
        {
            return Start().Then(body);
        }

        public IStepBuilder<TData, WaitFor> WaitFor(string eventName, Expression<Func<TData, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null,
            Expression<Func<TData, bool>> cancelCondition = null)
        {
            return Start().WaitFor(eventName, eventKey, effectiveDate, cancelCondition);
        }

        public IStepBuilder<TData, WaitFor> WaitFor(string eventName, Expression<Func<TData, IStepExecutionContext, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null,
            Expression<Func<TData, bool>> cancelCondition = null)
        {
            return Start().WaitFor(eventName, eventKey, effectiveDate, cancelCondition);
        }

        public IStepBuilder<TData, Delay> Delay(Expression<Func<TData, TimeSpan>> period)
        {
            return Start().Delay(period);
        }

        public IStepBuilder<TData, Decide> Decide(Expression<Func<TData, object>> expression)
        {
            return Start().Decide(expression);
        }

        public IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection)
        {
            return Start().ForEach(collection);
        }

        public IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection, Expression<Func<TData, bool>> runParallel)
        {
            return Start().ForEach(collection, runParallel);
        }

        public IContainerStepBuilder<TData, While, While> While(Expression<Func<TData, bool>> condition)
        {
            return Start().While(condition);
        }

        public IContainerStepBuilder<TData, If, If> If(Expression<Func<TData, bool>> condition)
        {
            return Start().If(condition);
        }

        public IContainerStepBuilder<TData, When, OutcomeSwitch> When(Expression<Func<TData, object>> outcomeValue, string label = null)
        {
            return ((IWorkflowModifier<TData, InlineStepBody>) Start()).When(outcomeValue, label);
        }

        public IParallelStepBuilder<TData, Sequence> Parallel()
        {
            return Start().Parallel();
        }

        public IStepBuilder<TData, Sequence> Saga(Action<IWorkflowBuilder<TData>> builder)
        {
            return Start().Saga(builder);
        }

        public IContainerStepBuilder<TData, Schedule, InlineStepBody> Schedule(Expression<Func<TData, TimeSpan>> time)
        {
            return Start().Schedule(time);
        }

        public IContainerStepBuilder<TData, Recur, InlineStepBody> Recur(Expression<Func<TData, TimeSpan>> interval, Expression<Func<TData, bool>> until)
        {
            return Start().Recur(interval, until);
        }

        public IStepBuilder<TData, Activity> Activity(string activityName, Expression<Func<TData, object>> parameters = null, Expression<Func<TData, DateTime>> effectiveDate = null,
            Expression<Func<TData, bool>> cancelCondition = null)
        {
            return Start().Activity(activityName, parameters, effectiveDate, cancelCondition);
        }

        private IStepBuilder<TData, InlineStepBody> Start()
        {
            return StartWith(_ => ExecutionResult.Next());
        }
    }
}
