using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Primitives;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Users.Interface
{
    public interface IUserTaskBuilder<TData> : IStepBuilder<TData, UserTask>
    {
        IUserTaskReturnBuilder<TData> WithOption(string value, string label);
        
    }
}
