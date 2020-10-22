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
                    return ExecutionResult.Branch(new List<object>() { context.Item }, new ControlPersistenceData() { ChildrenActive = true });
                }

                return ExecutionResult.Next();
            }

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            {
                if (!context.Workflow.IsBranchComplete(context.ExecutionPointer.Id))
                    return ExecutionResult.Persist(context.PersistenceData);
                
                return ExecutionResult.Persist(null);  //re-evaluate condition on next pass
            }

            throw new CorruptPersistenceDataException();
        }        
    }
}
