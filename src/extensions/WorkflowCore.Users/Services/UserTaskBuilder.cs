using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Services;
using WorkflowCore.Users.Interface;
using WorkflowCore.Users.Models;
using WorkflowCore.Users.Primitives;

namespace WorkflowCore.Users.Services
{
    public class UserTaskBuilder<TData> : StepBuilder<TData, UserTask<TData>>, IUserTaskBuilder<TData>
    {
        private readonly WorkflowStep<UserTask<TData>> _step;

        public UserTaskBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<UserTask<TData>> step) 
            : base (workflowBuilder, step)
        {
            _step = step;
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
            _step.PreExectionActions.Add((userTask, context, pointer) =>
            {
                userTask.Options[label] = value;
            });

            return stepBuilder;
        }

        public IUserTaskBuilder<TData> WithEscalation(Expression<Func<TData, TimeSpan>> after, Expression<Func<TData, string>> newUser, Action<IWorkflowBuilder<TData>> action = null)
        {
            var newStep = new EscalateStep();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Escalate>(WorkflowBuilder, newStep);
            stepBuilder.Input(step => step.TimeOut, after);
            stepBuilder.Input(step => step.NewUser, newUser);

            _step.PreExectionActions.Add((userTask, context, pointer) =>
            {
                userTask.Escalations.Add(new Escalation<TData>()
                {
                    TimeOut = after,
                    NewUser = newUser
                });
            });

            if (action != null)
            {
                var lastStep = WorkflowBuilder.LastStep;
                action.Invoke(WorkflowBuilder);
                if (WorkflowBuilder.LastStep > lastStep)
                    newStep.Outcomes.Add(new StepOutcome() { NextStep = lastStep + 1 });
            }

            return this;
        }
    }
}
