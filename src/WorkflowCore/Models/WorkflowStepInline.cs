using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class WorkflowStepInline : WorkflowStep<InlineStepBody>
    {
        public Func<IStepExecutionContext, ExecutionResult> Body { get; set; }
    }
}
