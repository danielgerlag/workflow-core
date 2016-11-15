using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class StepOutcomeBuilder<TData> : IStepOutcomeBuilder<TData>
    {
        private readonly IWorkflowBuilder<TData> _workflowBuilder;
        private StepOutcome _outcome;

        public StepOutcomeBuilder(IWorkflowBuilder<TData> workflowBuilder, StepOutcome outcome)
        {
            _workflowBuilder = workflowBuilder;
            _outcome = outcome;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> step = new WorkflowStep<TStep>();
            _workflowBuilder.AddStep(step);
            var stepBuilder = new StepBuilder<TData, TStep>(_workflowBuilder, step);

            if (stepSetup != null)
                stepSetup.Invoke(stepBuilder);

            step.Name = step.Name ?? typeof(TStep).Name;
            _outcome.NextStep = step.Id;

            return stepBuilder;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> step)
            where TStep : IStepBody
        {
            _outcome.NextStep = step.Step.Id;
            var stepBuilder = new StepBuilder<TData, TStep>(_workflowBuilder, step.Step);
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            _workflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(_workflowBuilder, newStep);
            _outcome.NextStep = newStep.Id;
            return stepBuilder;
        }
    }
}
