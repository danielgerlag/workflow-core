using System;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{   

    public interface IStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        WorkflowStep<TStepBody> Step { get; set; }
        IStepBuilder<TData, TStepBody> Name(string name);
        IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;
        IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> newStep) where TStep : IStepBody;
        IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body);
        IStepOutcomeBuilder<TData> When(object outcomeValue);        
        IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, TInput>> value);
        IStepBuilder<TData, TStepBody> Output<TOutput>(Expression<Func<TData, TOutput>> dataProperty, Expression<Func<TStepBody, TOutput>> value);

        IStepBuilder<TData, SubscriptionStepBody> WaitFor(string eventName, string eventKey);
    }
    

}