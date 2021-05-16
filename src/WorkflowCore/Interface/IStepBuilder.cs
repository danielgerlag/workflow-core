using System;
using System.Linq.Expressions;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IStepBuilder<TData, TStepBody> : IWorkflowModifier<TData, TStepBody>
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
        /// Specifies a custom Id to reference this step
        /// </summary>
        /// <param name="id">A custom Id to reference this step</param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Id(string id);

        /// <summary>
        /// Specify the next step in the workflow by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Attach(string id);

        /// <summary>
        /// Configure an outcome for this step, then wire it to another step
        /// </summary>
        /// <param name="outcomeValue"></param>
        /// <returns></returns>
        [Obsolete]
        IStepOutcomeBuilder<TData> When(object outcomeValue, string label = null);

        /// <summary>
        /// Configure an outcome branch for this step, then wire it to another step
        /// </summary>
        /// <param name="outcomeValue"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Branch<TStep>(object outcomeValue, IStepBuilder<TData, TStep> branch) where TStep : IStepBody;

        /// <summary>
        /// Configure an outcome branch for this step, then wire it to another step
        /// </summary>
        /// <param name="outcomeExpression"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Branch<TStep>(Expression<Func<TData, object, bool>> outcomeExpression, IStepBuilder<TData, TStep> branch) where TStep : IStepBody;

        /// <summary>
        /// Map properties on the step to properties on the workflow data object before the step executes
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="stepProperty">Property on the step</param>
        /// <param name="value"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, TInput>> value);

        /// <summary>
        /// Map properties on the step to properties on the workflow data object before the step executes
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="stepProperty">The property on the step</param>
        /// <param name="value"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, IStepExecutionContext, TInput>> value);

        /// <summary>
        /// Manipulate properties on the step before its executed.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Input(Action<TStepBody, TData> action);
        IStepBuilder<TData, TStepBody> Input(Action<TStepBody, TData, IStepExecutionContext> action);

        /// <summary>
        /// Map properties on the workflow data object to properties on the step after the step executes
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="dataProperty">Property on the data object</param>
        /// <param name="value"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Output<TOutput>(Expression<Func<TData, TOutput>> dataProperty, Expression<Func<TStepBody, object>> value);

        /// <summary>
        /// Manipulate properties on the data object after the step executes
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> Output(Action<TStepBody, TData> action);

        IStepBuilder<TData, TStep> End<TStep>(string name) where TStep : IStepBody;

        /// <summary>
        /// Configure the behavior when this step throws an unhandled exception
        /// </summary>
        /// <param name="behavior">What action to take when this step throws an unhandled exception</param>
        /// <param name="retryInterval">If the behavior is retry, how often</param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> OnError(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null);

        /// <summary>
        /// Ends the workflow and marks it as complete
        /// </summary>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> EndWorkflow();

        /// <summary>
        /// Undo step if unhandled exception is thrown by this step
        /// </summary>
        /// <typeparam name="TStep">The type of the step to execute</typeparam>
        /// <param name="stepSetup">Configure additional parameters for this step</param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> CompensateWith<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;

        /// <summary>
        /// Undo step if unhandled exception is thrown by this step
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> CompensateWith(Func<IStepExecutionContext, ExecutionResult> body);

        /// <summary>
        /// Undo step if unhandled exception is thrown by this step
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> CompensateWith(Action<IStepExecutionContext> body);

        /// <summary>
        /// Undo step if unhandled exception is thrown by this step
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> CompensateWithSequence(Action<IWorkflowBuilder<TData>> builder);

        /// <summary>
        /// Prematurely cancel the execution of this step on a condition
        /// </summary>
        /// <param name="cancelCondition"></param>
        /// <returns></returns>
        IStepBuilder<TData, TStepBody> CancelCondition(Expression<Func<TData, bool>> cancelCondition, bool proceedAfterCancel = false);
    }
}
