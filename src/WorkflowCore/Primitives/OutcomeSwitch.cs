using System.Linq;
using System.Collections.Generic;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class OutcomeSwitch : ContainerStepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                var result = ExecutionResult.Branch(new List<object>() { null }, new ControlPersistenceData() { ChildrenActive = true });
                result.OutcomeValue = GetPreviousOutcome(context);
                return result;
            }

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
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
                else
                {
                    var result = ExecutionResult.Persist(context.PersistenceData);
                    result.OutcomeValue = GetPreviousOutcome(context);
                    return result;
                }
            }

            throw new CorruptPersistenceDataException();
        }

        private object GetPreviousOutcome(IStepExecutionContext context)
        {
            var prevPointer = context.Workflow.ExecutionPointers.First(x => x.Id == context.ExecutionPointer.PredecessorId);
            return prevPointer.Outcome;
        }
    }
}
