using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Services
{
    public class StepOutcomeBuilder<TData> : IStepOutcomeBuilder<TData>
    {
        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }
        public ValueOutcome Outcome { get; private set; }
        
        public StepOutcomeBuilder(IWorkflowBuilder<TData> workflowBuilder, ValueOutcome outcome)
        {
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> step = new WorkflowStep<TStep>();
            WorkflowBuilder.AddStep(step);
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, step);

            if (stepSetup != null)
            {
                stepSetup.Invoke(stepBuilder);
            }

            step.Name = step.Name ?? typeof(TStep).Name;
            Outcome.NextStep = step.Id;

            return stepBuilder;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> step)
            where TStep : IStepBody
        {
            Outcome.NextStep = step.Step.Id;
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, step.Step);
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(WorkflowBuilder, newStep);
            Outcome.NextStep = newStep.Id;
            return stepBuilder;
        }

        public void EndWorkflow()
        {
            EndStep newStep = new EndStep();
            WorkflowBuilder.AddStep(newStep);
            Outcome.NextStep = newStep.Id;
        }
    }
}
