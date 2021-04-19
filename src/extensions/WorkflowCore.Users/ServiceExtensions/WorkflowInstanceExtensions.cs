using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Users.Models;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Models
{
    public static class WorkflowInstanceExtensions
    {
        public static IEnumerable<OpenUserAction> GetOpenUserActions(this WorkflowInstance workflow)
        {
            List<OpenUserAction> result = new List<OpenUserAction>();
            var pointers = workflow.ExecutionPointers.Where(x => !x.EventPublished && x.EventName == UserTask.EventName).ToList();
            foreach (var pointer in pointers)
            {
                var item = new OpenUserAction
                {
                    Key = pointer.EventKey,
                    Prompt = Convert.ToString(pointer.ExtensionAttributes[UserTask.ExtPrompt]),
                    AssignedPrincipal = Convert.ToString(pointer.ExtensionAttributes[UserTask.ExtAssignPrincipal]),
                    Options = (pointer.ExtensionAttributes[UserTask.ExtUserOptions] as Dictionary<string, string>)
                };                                

                result.Add(item);
            }

            return result;
        }
    }
}
