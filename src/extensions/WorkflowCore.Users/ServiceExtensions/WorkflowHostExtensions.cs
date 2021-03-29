using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Users.Models;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Interface
{
    public static class WorkflowHostExtensions
    {
        public static async Task PublishUserAction(this IWorkflowHost host, string actionKey, string user, object value)
        {
            UserAction data = new UserAction
            {
                User = user,
                OutcomeValue = value
            };

            await host.PublishEvent(UserTask.EventName, actionKey, data);
        }

        public static IEnumerable<OpenUserAction> GetOpenUserActions(this IWorkflowHost host, string workflowId)
        {
            var workflow = host.PersistenceStore.GetWorkflowInstance(workflowId).Result;
            return workflow.GetOpenUserActions();
        }
    }
}
