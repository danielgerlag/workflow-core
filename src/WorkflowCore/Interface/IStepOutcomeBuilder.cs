﻿using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Interface
{
    public interface IStepOutcomeBuilder<TData>
    {
        IWorkflowBuilder<TData> WorkflowBuilder { get; }

        ValueOutcome Outcome { get; }

        IStepBuilder<TData, TStep> Then<
#if NET8_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
            TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;

        IStepBuilder<TData, TStep> Then<
#if NET8_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
            TStep>(IStepBuilder<TData, TStep> step) where TStep : IStepBody;

        IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body);

        void EndWorkflow();
    }
}