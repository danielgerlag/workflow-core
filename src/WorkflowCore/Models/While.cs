using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class While : StepBody
    {        
        public bool ConditionResult { get; set; }                

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (ConditionResult)
            {                
                return ExecutionResult.Branch(new List<object>() { null }, null);
            }

            bool complete = true;
            foreach (var childId in context.ExecutionPointer.Children)
                complete = complete && IsBranchComplete(context.Workflow.ExecutionPointers, childId);

            if (complete)
                return ExecutionResult.Next();

            return ExecutionResult.Persist(context.PersistenceData);
        }

        private bool IsBranchComplete(IEnumerable<ExecutionPointer> pointers, string rootId)
        {
            var root = pointers.First(x => x.Id == rootId);

            if (root.EndTime == null)
                return false;

            var list = pointers.Where(x => x.PredecessorId == rootId).ToList();

            bool result = true;

            foreach (var item in list)
                result = result && IsBranchComplete(pointers, item.Id);

            return result;
        }

    }
}
