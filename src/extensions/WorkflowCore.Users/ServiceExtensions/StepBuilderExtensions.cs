using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Interface
{
    public static class StepBuilderExtensions
    {
        public static IStepBuilder<TData, UserStep> UserStep<TData, TStepBody>(this IStepBuilder<TData, TStepBody> builder, string userPrompt, Expression<Func<TData, string>> assigner, Action<IStepBuilder<TData, UserStep>> stepSetup = null)
            where TStepBody : IStepBody
        {
            var newStep = new UserStepContainer();
            newStep.Principal = assigner;
            newStep.UserPrompt = userPrompt;            
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, UserStep>(builder.WorkflowBuilder, newStep);

            if (stepSetup != null)
                stepSetup.Invoke(stepBuilder);
            newStep.Name = newStep.Name ?? typeof(UserStepContainer).Name;

            builder.Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public static IStepBuilder<TData, UserStep> UserStep<TData>(this IStepOutcomeBuilder<TData> builder, string userPrompt, Expression<Func<TData, string>> assigner, Action<IStepBuilder<TData, UserStep>> stepSetup = null)
        {
            var newStep = new UserStepContainer();
            newStep.Principal = assigner;
            newStep.UserPrompt = userPrompt;
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, UserStep>(builder.WorkflowBuilder, newStep);

            if (stepSetup != null)
                stepSetup.Invoke(stepBuilder);
            newStep.Name = newStep.Name ?? typeof(UserStepContainer).Name;

            builder.Outcome.NextStep = newStep.Id;
            return stepBuilder;
        }
    }
}
