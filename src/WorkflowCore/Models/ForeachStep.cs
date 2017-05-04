using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class ForeachStep : WorkflowStep<ForeachStepBody>
    {
        public LambdaExpression CollectionExpression { get; set; }

        private IEnumerable _collectionResult;

        public override ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            _collectionResult = (IEnumerable)CollectionExpression.Compile().DynamicInvoke(workflow.Data);
            return base.InitForExecution(executorResult, defintion, workflow, executionPointer);
        }

        public override IStepBody ConstructBody(IServiceProvider serviceProvider)
        {            
            return new ForeachStepBody(_collectionResult);
        }

    }
}
