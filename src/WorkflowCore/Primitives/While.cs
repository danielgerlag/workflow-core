using System.Collections.Generic;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class While : ContainerStepBody
    {
        public bool Condition { get; set; }                

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                if (Condition)
                {
                    return ExecutionResult.Branch(new List<object>() { null }, new ControlPersistenceData() { ChildrenActive = true });
                }

                return ExecutionResult.Next();
            }

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            {
                for (int i = context.ExecutionPointer.Children.Count - 1; i > -1; i--)
                {
                    if (!IsBranchComplete(context.Workflow.ExecutionPointers, context.ExecutionPointer.Children[i]))                 
                        return ExecutionResult.Persist(context.PersistenceData);                 
                }
                
                return ExecutionResult.Persist(null);  //re-evaluate condition on next pass
            }

            throw new CorruptPersistenceDataException();
        }        
    }
}
