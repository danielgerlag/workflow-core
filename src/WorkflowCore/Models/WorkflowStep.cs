using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class WorkflowStep
    {
        public abstract Type BodyType { get; }

        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Tag { get; set; }

        public virtual List<int> Children { get; set; } = new List<int>();

        public virtual List<StepOutcome> Outcomes { get; set; } = new List<StepOutcome>();

        public virtual List<DataMapping> Inputs { get; set; } = new List<DataMapping>();

        public virtual List<DataMapping> Outputs { get; set; } = new List<DataMapping>();

        public virtual WorkflowErrorHandling? ErrorBehavior { get; set; }

        public virtual TimeSpan? RetryInterval { get; set; }

        public virtual int? CompensationStepId { get; set; }

        public virtual bool ResumeChildrenAfterCompensation => true;

        public virtual bool RevertChildrenAfterCompensation => false;

        public virtual LambdaExpression CancelCondition { get; set; }

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

        public virtual void PrimeForRetry(ExecutionPointer pointer)
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
            if (CancelCondition != null)
            {
                var func = CancelCondition.Compile();
                if ((bool)(func.DynamicInvoke(workflow.Data)))
                {
                    var toCancel = new Stack<ExecutionPointer>();
                    toCancel.Push(executionPointer);

                    while (toCancel.Count > 0)
                    {
                        var ptr = toCancel.Pop();

                        ptr.EndTime = DateTime.Now.ToUniversalTime();
                        ptr.Active = false;
                        ptr.Status = PointerStatus.Cancelled;

                        foreach (var childId in ptr.Children)
                        {
                            var child = workflow.ExecutionPointers.FindById(childId);
                            if (child != null)
                                toCancel.Push(child);
                        }
                    }
                }
            }
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
