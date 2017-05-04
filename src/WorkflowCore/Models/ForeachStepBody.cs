using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class ForeachStepBody : StepBody
    {
        public IEnumerable Collection { get; set; } 

        public ForeachStepBody(IEnumerable collection)
        {
            Collection = collection;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
