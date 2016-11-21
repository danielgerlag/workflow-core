using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class StepBuilder<TData, TStepBody> : IStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        private readonly IWorkflowBuilder<TData> _workflowBuilder;

        public WorkflowStep<TStepBody> Step { get; set; }

        public StepBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<TStepBody> step)
        {
            _workflowBuilder = workflowBuilder;
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
            _workflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, TStep>(_workflowBuilder, newStep);

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
            var stepBuilder = new StepBuilder<TData, TStep>(_workflowBuilder, newStep.Step);
            return stepBuilder;
        }


        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {            
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            _workflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(_workflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepOutcomeBuilder<TData> When(object outcomeValue)
        {
            StepOutcome result = new StepOutcome();
            result.Value = outcomeValue;
            Step.Outcomes.Add(result);
            var outcomeBuilder = new StepOutcomeBuilder<TData>(_workflowBuilder, result);
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

        public IStepBuilder<TData, SubscriptionStepBody> WaitFor(string eventName, string eventKey)
        {
            var newStep = new SubscriptionStep<SubscriptionStepBody>();
            newStep.EventName = eventName;
            newStep.EventKey = eventKey;
            _workflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, SubscriptionStepBody>(_workflowBuilder, newStep);
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

            return new StepBuilder<TData, TStep>(_workflowBuilder, (ancestor as WorkflowStep<TStep>));
        }

        private WorkflowStep IterateParents(int id, string name)
        {
            //todo: filter out circular paths
            var upstream = _workflowBuilder.GetUpstreamSteps(id);
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

    }
}
