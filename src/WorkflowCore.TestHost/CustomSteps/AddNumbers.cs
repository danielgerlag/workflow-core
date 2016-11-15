using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestHost.CustomSteps
{
    public class AddNumbers : StepBody
    {
        public int Input1 { get; set; }

        public int Input2 { get; set; }

        public int Output { get; set; }


        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Output = (Input1 + Input2);
            return OutcomeResult(null);
        }
    }
}
