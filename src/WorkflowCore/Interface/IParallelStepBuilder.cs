using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using WorkflowCore.Primitives;

namespace WorkflowCore.Interface
{
    public interface IParallelStepBuilder<TData,
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TStepBody>
        where TStepBody : IStepBody
    {
        IParallelStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder);
        IStepBuilder<TData, Sequence> Join();
    }
}
