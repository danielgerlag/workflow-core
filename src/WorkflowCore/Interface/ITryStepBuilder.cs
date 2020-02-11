using System;
using System.Collections.Generic;

namespace WorkflowCore.Interface
{
    public interface ITryStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        ICatchStepBuilder<TData, TStepBody> Catch<TStep>(IEnumerable<Type> exceptionTypes, Action<IStepBuilder<TData, TStep>> stepSetup = null) 
            where TStep : IStepBody;

        ICatchStepBuilder<TData, TStepBody> Catch(IEnumerable<Type> exceptionTypes, Action<IStepExecutionContext> body);
    }
}