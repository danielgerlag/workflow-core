using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class WorkflowStep
    {
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        public abstract Type BodyType { get; }

        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string ExternalId { get; set; }

        public virtual List<int> Children { get; set; } = new List<int>();

        public virtual List<IStepOutcome> Outcomes { get; set; } = new List<IStepOutcome>();

        public virtual List<IStepParameter> Inputs { get; set; } = new List<IStepParameter>();

        public virtual List<IStepParameter> Outputs { get; set; } = new List<IStepParameter>();

        public virtual WorkflowErrorHandling? ErrorBehavior { get; set; }

        public virtual TimeSpan? RetryInterval { get; set; }

        public virtual int? CompensationStepId { get; set; }

        public virtual bool ResumeChildrenAfterCompensation => true;

        public virtual bool RevertChildrenAfterCompensation => false;

        public virtual LambdaExpression CancelCondition { get; set; }

        public bool ProceedOnCancel { get; set; } = false;

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
        /// every execution pointer with no end time, even if this step was not executed in this round
        /// </summary>
        /// <param name="executorResult"></param>
        /// <param name="definition"></param>
        /// <param name="workflow"></param>
        /// <param name="executionPointer"></param>
        public virtual void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition definition, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
        }

        public virtual IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(BodyType) is IStepBody body)
            {
                return body;
            }

            var stepCtor = BodyType.GetConstructor(Array.Empty<Type>());
            return stepCtor != null ? stepCtor.Invoke(Array.Empty<object>()) as IStepBody : null;
        }
    }

    public class WorkflowStep<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TStepBody> : WorkflowStep
        where TStepBody : IStepBody
    {
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        public override Type BodyType => typeof(TStepBody);
    }

    public enum ExecutionPipelineDirective
    {
        Next = 0,
        Defer = 1,
        EndWorkflow = 2
    }
}
