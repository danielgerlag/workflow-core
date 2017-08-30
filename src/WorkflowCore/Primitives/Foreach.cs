using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class Foreach : ContainerStepBody
    {
        public IEnumerable Collection { get; set; }                

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                var values = Collection.Cast<object>();
                return ExecutionResult.Branch(new List<object>(values), new ControlPersistenceData() { ChildrenActive = true });
            }

            if (context.PersistenceData is ControlPersistenceData)
            {
                if ((context.PersistenceData as ControlPersistenceData).ChildrenActive)
                {
                    bool complete = true;
                    foreach (var childId in context.ExecutionPointer.Children)
                    {
                        complete = complete && IsBranchComplete(context.Workflow.ExecutionPointers, childId);
                    }

                    if (complete)
                    {
                        return ExecutionResult.Next();
                    }
                }
            }

            return ExecutionResult.Persist(context.PersistenceData);
        }        
    }
}
