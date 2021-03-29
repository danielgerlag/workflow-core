using System;
using System.Linq;
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
