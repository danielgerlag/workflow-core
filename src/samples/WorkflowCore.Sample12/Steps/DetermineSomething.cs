using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample12
{    
    public class DetermineSomething : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Outcome(2);
        }
    }
}
