using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
