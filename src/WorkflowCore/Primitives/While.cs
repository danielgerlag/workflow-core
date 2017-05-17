using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class While : ContainerStepBody
    {
        public bool ConditionResult { get; set; }                

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                if (ConditionResult)
                    return ExecutionResult.Branch(new List<object>() { null }, new ControlPersistenceData() { ChildrenActive = true });
                else
                    return ExecutionResult.Next();
            }

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            { 
                bool complete = true;
                foreach (var childId in context.ExecutionPointer.Children)
                    complete = complete && IsBranchComplete(context.Workflow.ExecutionPointers, childId);

                if (complete)
                    return ExecutionResult.Persist(null);
                else
                    return ExecutionResult.Persist(context.PersistenceData);
            }

            throw new Exception("Corrupt persistence data");
        }        
    }
}
