using System;
using System.Linq;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class ActionParameter<TStepBody, TData> : IStepParameter
    {
        private readonly Action<TStepBody, TData, IStepExecutionContext> _action;
     
        public ActionParameter(Action<TStepBody, TData, IStepExecutionContext> action)
        {
            _action = action;
        }

        public ActionParameter(Action<TStepBody, TData> action)
        {
            _action = new Action<TStepBody, TData, IStepExecutionContext>((body, data, context) =>
            {
                action(body, data);
            });
        }

        private void Assign(object data, IStepBody step, IStepExecutionContext context)
        {
            _action.Invoke((TStepBody)step, (TData)data, context);
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
