using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class Foreach : StepBody
    {
        public enum LoopState { Running, Complete };

        public LambdaExpression CollectionExpression { get; set; }

        public Foreach(LambdaExpression collectionExpression)
        {
            CollectionExpression = collectionExpression;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData == null)
            {
                var values = (IEnumerable<object>)CollectionExpression.Compile().DynamicInvoke(context.Workflow.Data);
                return ExecutionResult.Branch(new List<object>(values), LoopState.Running);
            }

            if (context.PersistenceData is LoopState)
            {
                if ((LoopState)(context.PersistenceData) == LoopState.Running)
                {
                    //TODO
                }
            }

            return ExecutionResult.Persist(context.PersistenceData);
        }
    }
}
