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
            // ExecutionPointers can be mutated by the host while callers poll open actions.
            // Snapshot with a short retry so concurrent Add/Remove does not throw.
            List<ExecutionPointer> pointers = null;
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    pointers = workflow.ExecutionPointers
                        .Where(x => !x.EventPublished && x.EventName == UserTask.EventName)
                        .ToList();
                    break;
                }
                catch (InvalidOperationException) when (attempt < 5)
                {
                }
            }

            var result = new List<OpenUserAction>();
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
