using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Primitives
{
    public class UserTaskStep : WorkflowStep<UserTask>
    {

        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public List<EscalateStep> Escalations { get; set; } = new List<EscalateStep>();

        public override IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            return new UserTask(Options, Escalations);
        }
    }
}
