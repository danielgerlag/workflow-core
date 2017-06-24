using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Models
{
    [Obsolete]
    public class UserStep : StepBody
    {
        public UserAction UserAction { get; set; }

        public UserStep()
        {
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Outcome(UserAction.OutcomeValue);
        }
    }
}
