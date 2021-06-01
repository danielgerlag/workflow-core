using System;
using System.Collections;
using System.Linq.Expressions;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Interface
{
    public interface IWorkflowModifier<TData, TStepBody>
        where TStepBody : IStepBody

    {
    /// <summary>
    /// Specify the next step in the workflow
    /// </summary>
    /// <typeparam name="TStep">The type of the step to execute</typeparam>
    /// <param name="stepSetup">Configure additional parameters for this step</param>
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
    /// Specify an inline next step in the workflow
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    IStepBuilder<TData, ActionStepBody> Then(Action<IStepExecutionContext> body);

    /// <summary>
    /// Wait here until to specified event is published
    /// </summary>
    /// <param name="eventName">The name used to identify the kind of event to wait for</param>
    /// <param name="eventKey">A specific key value within the context of the event to wait for</param>
    /// <param name="effectiveDate">Listen for events as of this effective date</param>
    /// <param name="cancelCondition">A conditon that when true will cancel this WaitFor</param>
    /// <returns></returns>
    IStepBuilder<TData, WaitFor> WaitFor(string eventName, Expression<Func<TData, string>> eventKey,
        Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null);

    /// <summary>
    /// Wait here until to specified event is published
    /// </summary>
    /// <param name="eventName">The name used to identify the kind of event to wait for</param>
    /// <param name="eventKey">A specific key value within the context of the event to wait for</param>
    /// <param name="effectiveDate">Listen for events as of this effective date</param>
    /// <param name="cancelCondition">A conditon that when true will cancel this WaitFor</param>
    /// <returns></returns>
    IStepBuilder<TData, WaitFor> WaitFor(string eventName,
        Expression<Func<TData, IStepExecutionContext, string>> eventKey,
        Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null);

    /// <summary>
    /// Wait for a specified period
    /// </summary>
    /// <param name="period"></param>
    /// <returns></returns>
    IStepBuilder<TData, Delay> Delay(Expression<Func<TData, TimeSpan>> period);

    /// <summary>
    /// Evaluate an expression and take a different path depending on the value
    /// </summary>
    /// <param name="expression">Expression to evaluate for decision</param>
    /// <returns></returns>
    IStepBuilder<TData, Decide> Decide(Expression<Func<TData, object>> expression);

    /// <summary>
    /// Execute a block of steps, once for each item in a collection in a parallel foreach
    /// </summary>
    /// <param name="collection">Resolves a collection for iterate over</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection);
    
    /// <summary>
    /// Execute a block of steps, once for each item in a collection in a RunParallel foreach
    /// </summary>
    /// <param name="collection">Resolves a collection for iterate over</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection, Expression<Func<TData, bool>> runParallel);

    /// <summary>
    /// Repeat a block of steps until a condition becomes true
    /// </summary>
    /// <param name="condition">Resolves a condition to break out of the while loop</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, While, While> While(Expression<Func<TData, bool>> condition);

    /// <summary>
    /// Execute a block of steps if a condition is true
    /// </summary>
    /// <param name="condition">Resolves a condition to evaluate</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, If, If> If(Expression<Func<TData, bool>> condition);

    /// <summary>
    /// Configure an outcome for this step, then wire it to a sequence
    /// </summary>
    /// <param name="outcomeValue"></param>
    /// <returns></returns>
    IContainerStepBuilder<TData, When, OutcomeSwitch> When(Expression<Func<TData, object>> outcomeValue,
        string label = null);

    /// <summary>
    /// Execute multiple blocks of steps in parallel
    /// </summary>
    /// <returns></returns>
    IParallelStepBuilder<TData, Sequence> Parallel();

    /// <summary>
    /// Execute a sequence of steps in a container
    /// </summary>
    /// <returns></returns>
    IStepBuilder<TData, Sequence> Saga(Action<IWorkflowBuilder<TData>> builder);

    /// <summary>
    /// Schedule a block of steps to execute in parallel sometime in the future
    /// </summary>
    /// <param name="time">The time span to wait before executing the block</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, Schedule, TStepBody> Schedule(Expression<Func<TData, TimeSpan>> time);

    /// <summary>
    /// Schedule a block of steps to execute in parallel sometime in the future at a recurring interval
    /// </summary>
    /// <param name="interval">The time span to wait between recurring executions</param>
    /// <param name="until">Resolves a condition to stop the recurring task</param>
    /// <returns></returns>
    IContainerStepBuilder<TData, Recur, TStepBody> Recur(Expression<Func<TData, TimeSpan>> interval,
        Expression<Func<TData, bool>> until);

    /// <summary>
    /// Wait here until an external activity is complete
    /// </summary>
    /// <param name="activityName">The name used to identify the activity to wait for</param>
    /// <param name="parameters">The data to pass the external activity worker</param>
    /// <param name="effectiveDate">Listen for events as of this effective date</param>
    /// <param name="cancelCondition">A conditon that when true will cancel this WaitFor</param>
    /// <returns></returns>
    IStepBuilder<TData, Activity> Activity(string activityName, Expression<Func<TData, object>> parameters = null,
        Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null);

    /// <summary>
    /// Wait here until an external activity is complete
    /// </summary>
    /// <param name="activityName">The name used to identify the activity to wait for</param>
    /// <param name="parameters">The data to pass the external activity worker</param>
    /// <param name="effectiveDate">Listen for events as of this effective date</param>
    /// <param name="cancelCondition">A conditon that when true will cancel this WaitFor</param>
    /// <returns></returns>
    IStepBuilder<TData, Activity> Activity(Expression<Func<TData, IStepExecutionContext, string>> activityName, Expression<Func<TData, object>> parameters = null,
        Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null);
    }
}