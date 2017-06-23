using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Services;
using WorkflowCore.Users.Interface;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Users.Services
{
    public class UserTaskBuilder<TData> : StepBuilder<TData, UserTask>, IUserTaskBuilder<TData>
    {
        private UserTaskWrapper _wrapper;

        public UserTaskBuilder(IWorkflowBuilder<TData> workflowBuilder, UserTaskWrapper step) 
            : base (workflowBuilder, step)
        {
            _wrapper = step;
        }

        public IUserTaskReturnBuilder<TData> WithOption(string value, string label)
        {
            var newStep = new WorkflowStep<When>();
            Expression<Func<When, object>> inputExpr = (x => x.ExpectedOutcome);
            Expression<Func<TData, string>> valueExpr = (x => value);
            var mapping = new DataMapping()
            {
                Source = valueExpr,
                Target = inputExpr
            };
            newStep.Inputs.Add(mapping);

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new UserTaskReturnBuilder<TData>(WorkflowBuilder, newStep, this);

            Step.Children.Add(newStep.Id);
            _wrapper.Options[label] = value;

            return stepBuilder;
        }
    }
}
