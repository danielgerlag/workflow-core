using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Primitives
{
    public class UserTaskWrapper : WorkflowStep<UserTask>
    {

        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public override IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            return new UserTask(Options);
        }
    }
}
