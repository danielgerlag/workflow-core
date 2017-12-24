using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ReturnStepBuilder<TData, TStepBody, TParentStep> : IContainerStepBuilder<TData, TStepBody, TParentStep>
        where TStepBody : IStepBody
        where TParentStep : IStepBody
    {
        private readonly IStepBuilder<TData, TParentStep> _referenceBuilder;

        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }

        public WorkflowStep<TStepBody> Step { get; set; }

        public ReturnStepBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<TStepBody> step, IStepBuilder<TData, TParentStep> referenceBuilder)
        {
            WorkflowBuilder = workflowBuilder;
            Step = step;
            _referenceBuilder = referenceBuilder;
        }
        
        public IStepBuilder<TData, TParentStep> Do(Action<IWorkflowBuilder<TData>> builder)
        {
            builder.Invoke(WorkflowBuilder);
            Step.Children.Add(Step.Id + 1); //TODO: make more elegant

            return _referenceBuilder;
        }
    }
}
