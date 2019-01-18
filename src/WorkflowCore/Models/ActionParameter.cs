using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class ActionParameter<TStepBody, TData> : IStepParameter
    {
        private readonly Action<TStepBody, TData> _action;
     
        public ActionParameter(Action<TStepBody, TData> action)
        {
            _action = action;
        }

        private void Assign(object data, IStepBody step, IStepExecutionContext context)
        {
            _action.Invoke((TStepBody)step, (TData)data);
        }

        public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
        {
            Assign(data, body, context);
        }

        public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
        {
            Assign(data, body, context);
        }
    }
}
