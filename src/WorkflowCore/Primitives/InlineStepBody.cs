using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class InlineStepBody : StepBody
    {

        private readonly Func<IStepExecutionContext, ExecutionResult> _body;

        public InlineStepBody(Func<IStepExecutionContext, ExecutionResult> body)
        {
            _body = body;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return _body.Invoke(context);
        }
    }
}
