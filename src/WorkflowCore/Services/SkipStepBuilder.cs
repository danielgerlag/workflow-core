using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Services
{
    public class SkipStepBuilder<TData, TStepBody> : IContainerStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        private IStepBuilder<TData, TStepBody> _referenceBuilder;

        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }

        public WorkflowStep<TStepBody> Step { get; set; }

        public SkipStepBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<TStepBody> step, IStepBuilder<TData, TStepBody> referenceBuilder)
        {
            WorkflowBuilder = workflowBuilder;
            Step = step;
            _referenceBuilder = referenceBuilder;
        }
        
        public IStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder)
        {
            builder.Invoke(WorkflowBuilder);
            Step.Children.Add(Step.Id + 1); //TODO: make more elegant                        

            return this;
        }
    }
}
