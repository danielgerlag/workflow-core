using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Collections.Generic;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Interface
{
    public interface IWorkflowBuilder
    {
        List<WorkflowStep> Steps { get; }

        int LastStep { get; }

        IWorkflowBuilder<T> UseData<
#if NET8_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
            T>();

        WorkflowDefinition Build(string id, int version);

        void AddStep(WorkflowStep step);

        void AttachBranch(IWorkflowBuilder branch);
    }

    public interface IWorkflowBuilder<TData> : IWorkflowBuilder, IWorkflowModifier<TData, InlineStepBody>
    {
        IStepBuilder<TData, TStep> StartWith<
#if NET8_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
            TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;

        IStepBuilder<TData, InlineStepBody> StartWith(Func<IStepExecutionContext, ExecutionResult> body);

        IStepBuilder<TData, ActionStepBody> StartWith(Action<IStepExecutionContext> body);

        IEnumerable<WorkflowStep> GetUpstreamSteps(int id);

        IWorkflowBuilder<TData> UseDefaultErrorBehavior(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null);

        IWorkflowBuilder<TData> CreateBranch();
    }
}
