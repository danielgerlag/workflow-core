using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample05.Steps
{
    public class SleepStep : StepBody
    {
        
        public TimeSpan Period { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
                return ExecutionResult.Sleep(Period, new object());
            else
                return ExecutionResult.Next();
        }
    }
}
