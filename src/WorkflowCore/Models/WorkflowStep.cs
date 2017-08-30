using System;
using System.Collections.Generic;
using System.Reflection;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class WorkflowStep
    {
        public abstract Type BodyType { get; }

        public int Id { get; set; }

        public string Name { get; set; }

        public List<int> Children { get; set; } = new List<int>();

        public List<StepOutcome> Outcomes { get; set; } = new List<StepOutcome>();

        public List<DataMapping> Inputs { get; set; } = new List<DataMapping>();

        public List<DataMapping> Outputs { get; set; } = new List<DataMapping>();

        public WorkflowErrorHandling? ErrorBehavior { get; set; }

        public TimeSpan? RetryInterval { get; set; }                

        public virtual ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            return ExecutionPipelineDirective.Next;
        }

        public virtual ExecutionPipelineDirective BeforeExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
        {
            return ExecutionPipelineDirective.Next;
        }

        public virtual void AfterExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionResult stepResult, ExecutionPointer executionPointer)
        {            
        }

        /// <summary>
        /// Called after every workflow execution round,
        /// every exectuon pointer with no end time, even if this step was not executed in this round
        /// </summary>
        /// <param name="executorResult"></param>
        /// <param name="defintion"></param>
        /// <param name="workflow"></param>
        /// <param name="executionPointer"></param>
        public virtual void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
        }

        public virtual IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            IStepBody body = (serviceProvider.GetService(BodyType) as IStepBody);
            if (body == null)
            {
                var stepCtor = BodyType.GetConstructor(new Type[] { });
                if (stepCtor != null)
                    body = (stepCtor.Invoke(null) as IStepBody);
            }
            return body;
        }
    }

    public class WorkflowStep<TStepBody> : WorkflowStep
        where TStepBody : IStepBody 
    {
        public override Type BodyType => typeof(TStepBody);
    }

	public enum ExecutionPipelineDirective 
    { 
        Next = 0, 
        Defer = 1, 
        EndWorkflow = 2 
    }
}
