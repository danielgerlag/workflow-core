using System;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Users.Interface
{
    public interface IUserTaskBuilder<TData> : IStepBuilder<TData, UserTask>
    {
        /// <summary>
        /// Add a selectable option to the user task
        /// </summary>
        /// <param name="value">The key value for this option</param>
        /// <param name="label">The user facing label for this option</param>
        /// <returns></returns>
        IUserTaskReturnBuilder<TData> WithOption(string value, string label);

        /// <summary>
        /// Escalate this task to another user after a given period
        /// </summary>
        /// <param name="after">Period to wait before escalating</param>
        /// <param name="newUser">The user to escalate this task to</param>
        /// <param name="action"></param>
        /// <returns></returns>
        IUserTaskBuilder<TData> WithEscalation(Expression<Func<TData, TimeSpan>> after, Expression<Func<TData, string>> newUser, Action<IWorkflowBuilder<TData>> action = null);


    }
}
