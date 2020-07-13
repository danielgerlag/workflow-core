using System;

namespace WorkflowCore.Interface
{
    public interface ICatchStepBuilder<TData, TStepBody> : IStepBuilder<TData, TStepBody>, ITryStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
    }
}