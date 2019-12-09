using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public interface IWorkflowStep<out TStepBody> : IWorkflowStep
        where TStepBody : IStepBody
    {
    }

    public interface IWorkflowStep
    {
        Type BodyType { get; }
        int Id { get; set; }
        string Name { get; set; }
        string ExternalId { get; set; }
        List<int> Children { get; set; }
        List<StepOutcome> Outcomes { get; set; }
        List<IStepParameter> Inputs { get; set; }
        List<IStepParameter> Outputs { get; set; }
        WorkflowErrorHandling? ErrorBehavior { get; set; }
        TimeSpan? RetryInterval { get; set; }
        int? CompensationStepId { get; set; }
        bool ResumeChildrenAfterCompensation { get; }
        bool RevertChildrenAfterCompensation { get; }
        LambdaExpression CancelCondition { get; set; }
        bool ProceedOnCancel { get; set; }
        ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer);
        ExecutionPipelineDirective BeforeExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body);
        void AfterExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionResult stepResult, ExecutionPointer executionPointer);
        void PrimeForRetry(ExecutionPointer pointer);

        /// <summary>
        /// Called after every workflow execution round,
        /// every exectuon pointer with no end time, even if this step was not executed in this round
        /// </summary>
        /// <param name="executorResult"></param>
        /// <param name="defintion"></param>
        /// <param name="workflow"></param>
        /// <param name="executionPointer"></param>
        void AfterWorkflowIteration(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer);

        IStepBody ConstructBody(IServiceProvider serviceProvider);
    }
}