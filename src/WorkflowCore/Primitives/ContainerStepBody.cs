using System.Linq;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public abstract class ContainerStepBody : StepBody
    {
        protected bool IsBranchComplete(ICollection<ExecutionPointer> pointers, string rootId)
        {
            var root = pointers.First(x => x.Id == rootId);

            if (root.EndTime == null)
            {
                return false;
            }

            var queue = new Queue<ExecutionPointer>(pointers.Where(x => x.PredecessorId == rootId));

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (item.EndTime == null)
                {
                    return false;
                }

                var children = pointers.Where(x => x.PredecessorId == item.Id).ToList();
                foreach (var child in children)
                    queue.Enqueue(child);
            }

            return true;
        }
    }
}
