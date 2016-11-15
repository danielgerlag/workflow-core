using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{    

    public interface IStepOutcomeBuilder<TData>
    {
        IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;
        IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> step) where TStep : IStepBody;
        IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body);
    }

}