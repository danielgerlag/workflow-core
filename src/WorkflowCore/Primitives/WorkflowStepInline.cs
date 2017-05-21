using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class WorkflowStepInline : WorkflowStep<InlineStepBody>
    {
        public Func<IStepExecutionContext, ExecutionResult> Body { get; set; }

        public override IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            return new InlineStepBody(Body);
        }
    }
}
