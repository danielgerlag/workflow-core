using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Models
{
    public class UserStepBody : StepBody
    {
        public UserAction UserAction { get; set; }

        public UserStepBody()
        {
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return ExecutionResult.Outcome(UserAction.OutcomeValue);
        }
    }
}
