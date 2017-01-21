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
        public static IStepBuilder<TData, UserStepBody> UserStep<TData, TStepBody>(this IStepBuilder<TData, TStepBody> builder, string userPrompt, Expression<Func<TData, string>> assigner)
            where TStepBody : IStepBody
        {
            var newStep = new UserStep();
            newStep.Principal = assigner;
            newStep.UserPrompt = userPrompt;
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, UserStepBody>(builder.WorkflowBuilder, newStep);
            builder.Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }
    }
}
