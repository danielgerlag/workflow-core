using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Interface
{
    public interface IParentStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        IStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder);
    }
}
