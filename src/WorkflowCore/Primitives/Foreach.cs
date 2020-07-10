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
        public bool RunParallel { get; set; } = true;

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                var values = Collection.Cast<object>();
                if (RunParallel)
                {
                    return ExecutionResult.Branch(new List<object>(values), new IteratorPersistenceData() { ChildrenActive = true });
                }
                else
                {
                    return ExecutionResult.Branch(new List<object>(new object[] { values.ElementAt(0) }), new IteratorPersistenceData() { ChildrenActive = true });
                }
            }

            if (context.PersistenceData is IteratorPersistenceData persistanceData && persistanceData?.ChildrenActive == true)
            {
                if (context.Workflow.IsBranchComplete(context.ExecutionPointer.Id))
                {
                    if (!RunParallel)
                    {
                        var values = Collection.Cast<object>();
                        persistanceData.Index++;
                        if (persistanceData.Index < values.Count())
                        {
                            return ExecutionResult.Branch(new List<object>(new object[] { values.ElementAt(persistanceData.Index) }), persistanceData);
                        }
                    }

                    return ExecutionResult.Next();
                }

                return ExecutionResult.Persist(persistanceData);
            }

            return ExecutionResult.Persist(context.PersistenceData);
        }
    }
}
