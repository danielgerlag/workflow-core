using System;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class CancellableStep<TStepBody, TData> : WorkflowStep<TStepBody> 
        where TStepBody : IStepBody
    {
        private readonly Expression<Func<TData, bool>> _cancelCondition;

        public CancellableStep(Expression<Func<TData, bool>> cancelCondition)
        {
            _cancelCondition = cancelCondition;
        }
        
        public override void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            base.AfterWorkflowIteration(executorResult, defintion, workflow, executionPointer);
            var func = _cancelCondition.Compile();
            if (func((TData) workflow.Data))
            {
                executionPointer.EndTime = DateTime.Now.ToUniversalTime();
                executionPointer.Active = false;
            }
        }
    }
}
