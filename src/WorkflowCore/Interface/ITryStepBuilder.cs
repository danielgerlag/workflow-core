using System;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ITryStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        ICatchStepBuilder<TData, TStepBody> Catch<TStep>(IEnumerable<Type> exceptionTypes, Action<IStepBuilder<TData, TStep>> stepSetup = null) 
            where TStep : IStepBody;

        ICatchStepBuilder<TData, TStepBody> Catch(IEnumerable<Type> exceptionTypes, Action<IStepExecutionContext> body);

        ICatchStepBuilder<TData, TStepBody> Catch(IEnumerable<Type> exceptionTypes,
            Func<IStepExecutionContext, ExecutionResult> body);

        ICatchStepBuilder<TData, TStepBody> CatchWithSequence(IEnumerable<Type> exceptionTypes,
            Action<IWorkflowBuilder<TData>> builder);
    }
}