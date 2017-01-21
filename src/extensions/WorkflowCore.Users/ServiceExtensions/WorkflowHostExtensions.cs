using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Interface
{
    public static class WorkflowHostExtensions
    {
        public static async Task PublishUserAction(this IWorkflowHost host, string actionKey, string user, object value)
        {
            UserAction data = new UserAction()
            {
                User = user,
                OutcomeValue = value
            };

            await host.PublishEvent("UserAction", actionKey, data);
        }
    }
}
