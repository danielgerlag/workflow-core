using System;

namespace WorkflowCore.Interface
{
    public interface IContainerStepBuilder<TData, TStepBody, TReturnStep>
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
