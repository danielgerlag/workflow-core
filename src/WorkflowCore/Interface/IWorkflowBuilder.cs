using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowBuilder
    {
        int InitialStep { get; set; }                

        IWorkflowBuilder<T> UseData<T>();

        WorkflowDefinition Build(string id, int version);

        void AddStep(WorkflowStep step);
    }

    public interface IWorkflowBuilder<TData> : IWorkflowBuilder
    {        
        IStepBuilder<TData, TStep> StartWith<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody;
        IStepBuilder<TData, InlineStepBody> StartWith(Func<IStepExecutionContext, ExecutionResult> body);

        IEnumerable<WorkflowStep> GetUpstreamSteps(int id);
    }
        

}