using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class StepBuilder<TData, TStepBody> : IStepBuilder<TData, TStepBody>, IParentStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }

        public WorkflowStep<TStepBody> Step { get; set; }

        public StepBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<TStepBody> step)
        {
            WorkflowBuilder = workflowBuilder;
            Step = step;
        }

        public IStepBuilder<TData, TStepBody> Name(string name)
        {
            Step.Name = name;
            return this;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> newStep = new WorkflowStep<TStep>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, newStep);

            if (stepSetup != null)
                stepSetup.Invoke(stepBuilder);

            newStep.Name = newStep.Name ?? typeof(TStep).Name;
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> newStep)
            where TStep : IStepBody
        {
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Step.Id });
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, newStep.Step);
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {            
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(WorkflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepOutcomeBuilder<TData> When(object outcomeValue, string label = null)
        {
            StepOutcome result = new StepOutcome();
            result.Value = outcomeValue;
            result.Label = label;
            Step.Outcomes.Add(result);
            var outcomeBuilder = new StepOutcomeBuilder<TData>(WorkflowBuilder, result);
            return outcomeBuilder;
        }

        public IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, TInput>> value)
        {
            var mapping = new DataMapping();            
            mapping.Source = value;
            mapping.Target = stepProperty;
            Step.Inputs.Add(mapping);
            return this;
        }

        public IStepBuilder<TData, TStepBody> Output<TOutput>(Expression<Func<TData, TOutput>> dataProperty, Expression<Func<TStepBody, TOutput>> value)
        {
            var mapping = new DataMapping();
            mapping.Source = value;
            mapping.Target = dataProperty;
            Step.Outputs.Add(mapping);
            return this;
        }

        public IStepBuilder<TData, SubscriptionStepBody> WaitFor(string eventName, Expression<Func<TData, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null)
        {
            var newStep = new SubscriptionStep<SubscriptionStepBody>();
            newStep.EventName = eventName;
            newStep.EventKey = eventKey;
            newStep.EffectiveDate = effectiveDate;
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, SubscriptionStepBody>(WorkflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepBuilder<TData, TStep> End<TStep>(string name) where TStep : IStepBody
        {
            var ancestor = IterateParents(Step.Id, name);

            if (ancestor == null)
                throw new Exception(String.Format("Parent step of name {0} not found", name));

            if (!(ancestor is WorkflowStep<TStep>))
                throw new Exception(String.Format("Parent step of name {0} is not of type {1}", name, typeof(TStep)));

            return new StepBuilder<TData, TStep>(WorkflowBuilder, (ancestor as WorkflowStep<TStep>));
        }

        public IStepBuilder<TData, TStepBody> OnError(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null)
        {
            Step.ErrorBehavior = behavior;
            Step.RetryInterval = retryInterval;
            return this;
        }

        private WorkflowStep IterateParents(int id, string name)
        {
            //todo: filter out circular paths
            var upstream = WorkflowBuilder.GetUpstreamSteps(id);
            foreach (var parent in upstream)
            {
                if (parent.Name == name)
                    return parent;
            }

            foreach (var parent in upstream)
            {
                var result = IterateParents(parent.Id, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public IStepBuilder<TData, TStepBody> EndWorkflow()
        {
            EndStep newStep = new EndStep();
            WorkflowBuilder.AddStep(newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return this;
        }

        public IParentStepBuilder<TData, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection)
        {
            var newStep = new WorkflowStep<Foreach>();
            
            Expression<Func<Foreach, IEnumerable>> inputExpr = (x => x.Collection);

            var mapping = new DataMapping()
            {
                Source = collection,
                Target = inputExpr
            };
            newStep.Inputs.Add(mapping);            

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Foreach>(WorkflowBuilder, newStep);                        

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IParentStepBuilder<TData, While> While(Expression<Func<TData, bool>> condition)
        {
            var newStep = new WorkflowStep<While>();

            Expression<Func<While, bool>> inputExpr = (x => x.ConditionResult);

            var mapping = new DataMapping()
            {
                Source = condition,
                Target = inputExpr
            };
            newStep.Inputs.Add(mapping);

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, While>(WorkflowBuilder, newStep);

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder)
        {
            builder.Invoke(WorkflowBuilder);
            Step.Children.Add(Step.Id + 1); //TODO: make more elegant                        

            return this;
        }
    }
}
