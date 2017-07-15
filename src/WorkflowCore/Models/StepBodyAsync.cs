using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class StepBodyAsync : IStepBody
    {
        public abstract Task<ExecutionResult> RunAsync(IStepExecutionContext context);
        
    }
}
