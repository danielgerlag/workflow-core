using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Interface
{
    public interface IParallelStepBuilder<TData, TStepBody, TReturnStep>
        where TStepBody : IStepBody
        where TReturnStep : IStepBody
    {
        IParallelStepBuilder<TData, TStepBody, TReturnStep> Do(Action<IWorkflowBuilder<TData>> builder);
        IStepBuilder<TData, TReturnStep> Join();
    }
}
