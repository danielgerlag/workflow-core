using System;
using System.Linq.Expressions;
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
        private readonly UserTaskStep _wrapper;

        public UserTaskBuilder(IWorkflowBuilder<TData> workflowBuilder, UserTaskStep step) 
            : base (workflowBuilder, step)
        {
            _wrapper = step;
        }

        public IUserTaskReturnBuilder<TData> WithOption(string value, string label)
        {
            var newStep = new WorkflowStep<When>();
            Expression<Func<When, object>> inputExpr = (x => x.ExpectedOutcome);
            Expression<Func<TData, string>> valueExpr = (x => value);
            var mapping = new MemberMapParameter(valueExpr, inputExpr);
            newStep.Inputs.Add(mapping);

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new UserTaskReturnBuilder<TData>(WorkflowBuilder, newStep, this);

            Step.Children.Add(newStep.Id);
            _wrapper.Options[label] = value;

            return stepBuilder;
        }

        public IUserTaskBuilder<TData> WithEscalation(Expression<Func<TData, TimeSpan>> after, Expression<Func<TData, string>> newUser, Action<IWorkflowBuilder<TData>> action = null)
        {
            var newStep = new EscalateStep();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Escalate>(WorkflowBuilder, newStep);
            stepBuilder.Input(step => step.TimeOut, after);
            stepBuilder.Input(step => step.NewUser, newUser);

            _wrapper.Escalations.Add(newStep);

            if (action != null)
            {
                var lastStep = WorkflowBuilder.LastStep;
                action.Invoke(WorkflowBuilder);
                if (WorkflowBuilder.LastStep > lastStep)
                    newStep.Outcomes.Add(new ValueOutcome { NextStep = lastStep + 1 });
            }

            return this;
        }
    }
}
