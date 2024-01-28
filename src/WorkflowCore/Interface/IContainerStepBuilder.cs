using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace WorkflowCore.Interface
{
    public interface IContainerStepBuilder<TData,
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TStepBody,
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TReturnStep>
        where TStepBody : IStepBody
        where TReturnStep : IStepBody
    {
        /// <summary>
        /// The block of steps to execute
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        IStepBuilder<TData, TReturnStep> Do(Action<IWorkflowBuilder<TData>> builder);
    }
}
