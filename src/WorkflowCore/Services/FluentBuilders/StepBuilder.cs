using System;
using System.Collections;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace WorkflowCore.Services
{
    public class StepBuilder<TData, TStepBody> : IStepBuilder<TData, TStepBody>, IContainerStepBuilder<TData, TStepBody, TStepBody>
        where TStepBody : IStepBody
    {
        public IWorkflowBuilder<TData> WorkflowBuilder { get; private set; }

        public WorkflowStep<TStepBody> Step { get; set; }

        public StepBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<TStepBody> step)
        {
            WorkflowBuilder = workflowBuilder;
            Step = step;
        }

        public IStepBuilder<TData, TStepBody> Name(string name)
        {
            Step.Name = name;
            return this;
        }

        public IStepBuilder<TData, TStepBody> Id(string id)
        {
            Step.ExternalId = id;
            return this;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> newStep = new WorkflowStep<TStep>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, newStep);

            if (stepSetup != null)
            {
                stepSetup.Invoke(stepBuilder);
            }

            newStep.Name = newStep.Name ?? typeof(TStep).Name;
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IStepBuilder<TData, TStep> Then<TStep>(IStepBuilder<TData, TStep> newStep)
            where TStep : IStepBody
        {
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Step.Id });
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, newStep.Step);
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> Then(Func<IStepExecutionContext, ExecutionResult> body)
        {            
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(WorkflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepBuilder<TData, ActionStepBody> Then(Action<IStepExecutionContext> body)
        {
            var newStep = new WorkflowStep<ActionStepBody>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, ActionStepBody>(WorkflowBuilder, newStep);
            stepBuilder.Input(x => x.Body, x => body);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepBuilder<TData, TStepBody> Attach(string id)
        {
            Step.Outcomes.Add(new StepOutcome()
            {
                ExternalNextStepId = id
            });

            return this;
        }

        public IStepOutcomeBuilder<TData> When(object outcomeValue, string label = null)
        {
            StepOutcome result = new StepOutcome
            {
                Value = x => outcomeValue,
                Label = label
            };
            Step.Outcomes.Add(result);
            var outcomeBuilder = new StepOutcomeBuilder<TData>(WorkflowBuilder, result);
            return outcomeBuilder;
        }

        public IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, TInput>> value)
        {
            Step.Inputs.Add(new MemberMapParameter(value, stepProperty));
            return this;
        }

        public IStepBuilder<TData, TStepBody> Input<TInput>(Expression<Func<TStepBody, TInput>> stepProperty, Expression<Func<TData, IStepExecutionContext, TInput>> value)
        {
            Step.Inputs.Add(new MemberMapParameter(value, stepProperty));
            return this;
        }

        public IStepBuilder<TData, TStepBody> Input(Action<TStepBody, TData> action)
        {
            Step.Inputs.Add(new ActionParameter<TStepBody, TData>(action));
            return this;
        }

        public IStepBuilder<TData, TStepBody> Input(Action<TStepBody, TData, IStepExecutionContext> action)
        {
            Step.Inputs.Add(new ActionParameter<TStepBody, TData>(action));
            return this;
        }        

        public IStepBuilder<TData, TStepBody> Output<TOutput>(Expression<Func<TData, TOutput>> dataProperty, Expression<Func<TStepBody, object>> value)
        {
            Step.Outputs.Add(new MemberMapParameter(value, dataProperty));
            return this;
        }

        public IStepBuilder<TData, TStepBody> Output(Action<TStepBody, TData> action)
        {
            Step.Outputs.Add(new ActionParameter<TStepBody, TData>(action));
            return this;
        }

        public IStepBuilder<TData, WaitFor> WaitFor(string eventName, Expression<Func<TData, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null)
        {
            var newStep = new WorkflowStep<WaitFor>();
            newStep.CancelCondition = cancelCondition;

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, WaitFor>(WorkflowBuilder, newStep);
            stepBuilder.Input((step) => step.EventName, (data) => eventName);
            stepBuilder.Input((step) => step.EventKey, eventKey);

            if (effectiveDate != null)
            {
                stepBuilder.Input((step) => step.EffectiveDate, effectiveDate);
            }

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }

        public IStepBuilder<TData, WaitFor> WaitFor(string eventName, Expression<Func<TData, IStepExecutionContext, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null)
        {
            var newStep = new WorkflowStep<WaitFor>();
            newStep.CancelCondition = cancelCondition;

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, WaitFor>(WorkflowBuilder, newStep);
            stepBuilder.Input((step) => step.EventName, (data) => eventName);
            stepBuilder.Input((step) => step.EventKey, eventKey);

            if (effectiveDate != null)
            {
                stepBuilder.Input((step) => step.EffectiveDate, effectiveDate);
            }

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }
        
        public IStepBuilder<TData, TStep> End<TStep>(string name) where TStep : IStepBody
        {
            var ancestor = IterateParents(Step.Id, name);

            if (ancestor == null)
            {
                throw new InvalidOperationException($"Parent step of name {name} not found");
            }

            if (!(ancestor is WorkflowStep<TStep>))
            {
                throw new InvalidOperationException($"Parent step of name {name} is not of type {typeof(TStep)}");
            }

            return new StepBuilder<TData, TStep>(WorkflowBuilder, (ancestor as WorkflowStep<TStep>));
        }

        public IStepBuilder<TData, TStepBody> OnError(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null)
        {
            Step.ErrorBehavior = behavior;
            Step.RetryInterval = retryInterval;
            return this;
        }

        private WorkflowStep IterateParents(int id, string name)
        {
            // Todo: filter out circular paths
            var upstream = WorkflowBuilder.GetUpstreamSteps(id);
            foreach (var parent in upstream)
            {
                if (parent.Name == name)
                {
                    return parent;
                }
            }

            foreach (var parent in upstream)
            {
                var result = IterateParents(parent.Id, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public IStepBuilder<TData, TStepBody> EndWorkflow()
        {
            EndStep newStep = new EndStep();
            WorkflowBuilder.AddStep(newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return this;
        }

        public IStepBuilder<TData, Delay> Delay(Expression<Func<TData, TimeSpan>> period)
        {
            var newStep = new WorkflowStep<Delay>();

            Expression<Func<Delay, TimeSpan>> inputExpr = (x => x.Period);
            newStep.Inputs.Add(new MemberMapParameter(period, inputExpr));

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Delay>(WorkflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IContainerStepBuilder<TData, Foreach, Foreach> ForEach(Expression<Func<TData, IEnumerable>> collection)
        {
            var newStep = new WorkflowStep<Foreach>();
            
            Expression<Func<Foreach, IEnumerable>> inputExpr = (x => x.Collection);
            newStep.Inputs.Add(new MemberMapParameter(collection, inputExpr));            

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Foreach>(WorkflowBuilder, newStep);                        

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IContainerStepBuilder<TData, While, While> While(Expression<Func<TData, bool>> condition)
        {
            var newStep = new WorkflowStep<While>();

            Expression<Func<While, bool>> inputExpr = (x => x.Condition);
            newStep.Inputs.Add(new MemberMapParameter(condition, inputExpr));

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, While>(WorkflowBuilder, newStep);

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IContainerStepBuilder<TData, If, If> If(Expression<Func<TData, bool>> condition)
        {
            var newStep = new WorkflowStep<If>();

            Expression<Func<If, bool>> inputExpr = (x => x.Condition);
            newStep.Inputs.Add(new MemberMapParameter(condition, inputExpr));

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, If>(WorkflowBuilder, newStep);

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }
        
        public IContainerStepBuilder<TData, When, OutcomeSwitch> When(Expression<Func<TData, object>> outcomeValue, string label = null)
        {
            var newStep = new WorkflowStep<When>();
            Expression<Func<When, object>> inputExpr = (x => x.ExpectedOutcome);
            newStep.Inputs.Add(new MemberMapParameter(outcomeValue, inputExpr));

            IStepBuilder<TData, OutcomeSwitch> switchBuilder;

            if (Step.BodyType != typeof(OutcomeSwitch))
            {
                var switchStep = new WorkflowStep<OutcomeSwitch>();
                WorkflowBuilder.AddStep(switchStep);
                Step.Outcomes.Add(new StepOutcome()
                {
                    NextStep = switchStep.Id,
                    Label = label
                });
                switchBuilder = new StepBuilder<TData, OutcomeSwitch>(WorkflowBuilder, switchStep);
            }
            else
            {
                switchBuilder = (this as IStepBuilder<TData, OutcomeSwitch>);
            }
            
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new ReturnStepBuilder<TData, When, OutcomeSwitch>(WorkflowBuilder, newStep, switchBuilder);
            switchBuilder.Step.Children.Add(newStep.Id);

            return stepBuilder;
        }

        public IStepBuilder<TData, Sequence> Saga(Action<IWorkflowBuilder<TData>> builder)
        {
            var newStep = new SagaContainer<Sequence>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Sequence>(WorkflowBuilder, newStep);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            builder.Invoke(WorkflowBuilder);
            stepBuilder.Step.Children.Add(stepBuilder.Step.Id + 1); //TODO: make more elegant

            return stepBuilder;
        }

        public IParallelStepBuilder<TData, Sequence> Parallel()
        {
            var newStep = new WorkflowStep<Sequence>();
            var newBuilder = new StepBuilder<TData, Sequence>(WorkflowBuilder, newStep);
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new ParallelStepBuilder<TData, Sequence>(WorkflowBuilder, newBuilder, newBuilder);

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IContainerStepBuilder<TData, Schedule, TStepBody> Schedule(Expression<Func<TData, TimeSpan>> time)
        {
            var newStep = new WorkflowStep<Schedule>();
            Expression<Func<Schedule, TimeSpan>> inputExpr = (x => x.Interval);
            newStep.Inputs.Add(new MemberMapParameter(time, inputExpr));

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new ReturnStepBuilder<TData, Schedule, TStepBody>(WorkflowBuilder, newStep, this);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IContainerStepBuilder<TData, Recur, TStepBody> Recur(Expression<Func<TData, TimeSpan>> interval, Expression<Func<TData, bool>> until)
        {
            var newStep = new WorkflowStep<Recur>();
            newStep.CancelCondition = until;

            Expression<Func<Recur, TimeSpan>> intervalExpr = (x => x.Interval);
            Expression<Func<Recur, bool>> untilExpr = (x => x.StopCondition);
            newStep.Inputs.Add(new MemberMapParameter(interval, intervalExpr));
            newStep.Inputs.Add(new MemberMapParameter(until, untilExpr));

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new ReturnStepBuilder<TData, Recur, TStepBody>(WorkflowBuilder, newStep, this);
            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });

            return stepBuilder;
        }

        public IStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder)
        {
            builder.Invoke(WorkflowBuilder);
            Step.Children.Add(Step.Id + 1); //TODO: make more elegant

            return this;
        }

        public IStepBuilder<TData, TStepBody> CompensateWith<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null) where TStep : IStepBody
        {
            WorkflowStep<TStep> newStep = new WorkflowStep<TStep>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, TStep>(WorkflowBuilder, newStep);

            if (stepSetup != null)
            {
                stepSetup.Invoke(stepBuilder);
            }

            newStep.Name = newStep.Name ?? typeof(TStep).Name;
            Step.CompensationStepId = newStep.Id;

            return this;
        }

        public IStepBuilder<TData, TStepBody> CompensateWith(Func<IStepExecutionContext, ExecutionResult> body)
        {
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(WorkflowBuilder, newStep);
            Step.CompensationStepId = newStep.Id;
            return this;
        }

        public IStepBuilder<TData, TStepBody> CompensateWith(Action<IStepExecutionContext> body)
        {
            var newStep = new WorkflowStep<ActionStepBody>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, ActionStepBody>(WorkflowBuilder, newStep);
            stepBuilder.Input(x => x.Body, x => body);
            Step.CompensationStepId = newStep.Id;
            return this;
        }

        public IStepBuilder<TData, TStepBody> CompensateWithSequence(Action<IWorkflowBuilder<TData>> builder)
        {
            var newStep = new WorkflowStep<Sequence>();
            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Sequence>(WorkflowBuilder, newStep);
            Step.CompensationStepId = newStep.Id;
            builder.Invoke(WorkflowBuilder);
            stepBuilder.Step.Children.Add(stepBuilder.Step.Id + 1); //TODO: make more elegant

            return this;
        }

        public IStepBuilder<TData, TStepBody> CancelCondition(Expression<Func<TData, bool>> cancelCondition, bool proceedAfterCancel = false)
        {
            Step.CancelCondition = cancelCondition;
            Step.ProceedOnCancel = proceedAfterCancel;
            return this;
        }

        public IStepBuilder<TData, Activity> Activity(string activityName, Expression<Func<TData, object>> parameters = null, Expression<Func<TData, DateTime>> effectiveDate = null, Expression<Func<TData, bool>> cancelCondition = null)
        {
            var newStep = new WorkflowStep<Activity>();
            newStep.CancelCondition = cancelCondition;

            WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new StepBuilder<TData, Activity>(WorkflowBuilder, newStep);
            stepBuilder.Input((step) => step.ActivityName, (data) => activityName);
            
            if (parameters != null)
                stepBuilder.Input((step) => step.Parameters, parameters);
            
            if (effectiveDate != null)
                stepBuilder.Input((step) => step.EffectiveDate, effectiveDate);

            Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
            return stepBuilder;
        }
    }
}
