﻿using System;
using System.Collections;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Interface
{
    public interface IStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {

        IWorkflowBuilder<TData> WorkflowBuilder { get; }        

        WorkflowStep<TStepBody> Step { get; set; }

        /// <summary>
        /// Specifies a display name for the step
        /// </summary>
        /// <param name="name">A display name for the step for easy identification in logs, etc...</param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Name(string name);

        /// <summary>
        /// Specify the next step in the workflow
        /// </summary>
        /// <typeparam name="TStep"></typeparam>
        /// <param name="stepSetup"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;

        /// <summary>
        /// Specify the next step in the workflow
        /// </summary>
        /// <typeparam name="TStep"></typeparam>
        /// <param name="newStep"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> newStep) where TStep : IStepBody;

        /// <summary>
        /// Specify an inline next step in the workflow
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body);

        /// <summary>
        /// Configure an outcome for this step, then wire it to another step
        /// </summary>
        /// <param name="outcomeValue"></param>
        /// <returns></returns>
        [Obsolete]
        IStepOutcomeBuilder<TData> When(object outcomeValue, string label = null);

        /// <summary>
        /// Map properties on the step to properties on the workflow data object before the step executes
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="stepProperty">Property on the step</param>
        /// <param name="value"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, TInput>> value);

        /// <summary>
        /// Map properties on the workflow data object to properties on the step after the step executes
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="dataProperty">Property on the data object</param>
        /// <param name="value"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Output<TOutput>(Expression<Func<TData, TOutput>> dataProperty, Expression<Func<TStepBody, TOutput>> value);

        /// <summary>
        /// Put the workflow to sleep until to specified event is published
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="eventKey"></param>
        /// <returns></returns>
        IStepBuilder<TData, SubscriptionStepBody> WaitFor(string eventName, Expression<Func<TData, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null);

        IStepBuilder<TData, TStep> End<TStep>(string name) where TStep : IStepBody;

        IStepBuilder<TData, TStepBody> OnError(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null);

        /// <summary>
        /// Ends the workflow and marks it as complete
        /// </summary>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> EndWorkflow();

        IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection);

        IContainerStepBuilder<TData, While, While> While(Expression<Func<TData, bool>> condition);

        IContainerStepBuilder<TData, If, If> If(Expression<Func<TData, bool>> condition);

        /// <summary>
        /// Configure an outcome for this step, then wire it to a sequence
        /// </summary>
        /// <param name="outcomeValue"></param>
        /// <returns></returns>
        IContainerStepBuilder<TData, When, OutcomeSwitch> When(Expression<Func<TData, object>> outcomeValue, string label = null);
    }
}