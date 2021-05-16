using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Users.Interface
{
    public interface IUserTaskReturnBuilder<TData>
    {
        IWorkflowBuilder<TData> WorkflowBuilder { get; }
        WorkflowStep<When> Step { get; set; }
        IUserTaskBuilder<TData> Do(Action<IWorkflowBuilder<TData>> builder);
    }
}
