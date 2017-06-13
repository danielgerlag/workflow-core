﻿using System;
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
    public class ParallelStepBuilder<TData, TStepBody> : IParallelStepBuilder<TData, TStepBody>
        where TStepBody : IStepBody
    {
        private readonly IStepBuilder<TData, Sequence> _referenceBuilder;
        private readonly IStepBuilder<TData, TStepBody> _stepBuilder;

        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }

        public WorkflowStep<TStepBody> Step { get; set; }
        
        public ParallelStepBuilder(IWorkflowBuilder<TData> workflowBuilder, IStepBuilder<TData, TStepBody> stepBuilder, IStepBuilder<TData, Sequence> referenceBuilder)
        {
            WorkflowBuilder = workflowBuilder;
            Step = stepBuilder.Step;
            _stepBuilder = stepBuilder;
            _referenceBuilder = referenceBuilder;
        }
        
        public IParallelStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder)
        {
            int lastStep = WorkflowBuilder.LastStep;
            builder.Invoke(WorkflowBuilder);            
            Step.Children.Add(lastStep + 1); //TODO: make more elegant

            return this;
        }

        public IStepBuilder<TData, Sequence> Join()
        {
            return _referenceBuilder;
        }
    }
}
