using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class Sequence : ContainerStepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
                return ExecutionResult.Branch(new List<object>() { null }, new ControlPersistenceData() { ChildrenActive = true });

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            { 
                bool complete = true;
                foreach (var childId in context.ExecutionPointer.Children)
                    complete = complete && IsBranchComplete(context.Workflow.ExecutionPointers, childId);

                if (complete)
                    return ExecutionResult.Next();
                else
                    return ExecutionResult.Persist(context.PersistenceData);
            }

            throw new CorruptPersistenceDataException();
        }        
    }
}
