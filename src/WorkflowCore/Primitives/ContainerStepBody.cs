using System.Linq;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public abstract class ContainerStepBody : StepBody
    {       
        protected bool IsBranchComplete(IEnumerable<ExecutionPointer> pointers, string rootId)
        {
            //TODO: move to own class
            var root = pointers.First(x => x.Id == rootId);

            if (root.EndTime == null)
            {
                return false;   
            }

            var list = pointers.Where(x => x.PredecessorId == rootId).ToList();

            bool result = true;

            foreach (var item in list)
            {
                result = result && IsBranchComplete(pointers, item.Id);
            }

            return result;
        }
    }
}
