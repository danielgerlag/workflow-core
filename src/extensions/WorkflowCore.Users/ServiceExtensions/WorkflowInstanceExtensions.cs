using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Models
{
    public static class WorkflowInstanceExtensions
    {
        public static IEnumerable<OpenUserAction> GetOpenUserActions(this WorkflowInstance workflow)
        {
            List<OpenUserAction> result = new List<OpenUserAction>();
            var pointers = workflow.ExecutionPointers.Where(x => !x.EventPublished && x.EventName == "UserAction").ToList();
            foreach (var pointer in pointers)
            {
                var item = new OpenUserAction()
                {
                    Key = pointer.EventKey,
                    Prompt = Convert.ToString(pointer.ExtensionAttributes["Prompt"]),
                    AssignedPrincipal = Convert.ToString(pointer.ExtensionAttributes["AssignedPrincipal"])
                };

                result.Add(item);
            }

            return result;
        }
    }
}
