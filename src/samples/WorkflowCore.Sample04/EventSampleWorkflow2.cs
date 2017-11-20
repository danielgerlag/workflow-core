using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample04.Steps;

namespace WorkflowCore.Sample04
{
    public class EventSampleWorkflow2 : IWorkflow<MyDataClass>
    {
        public string Id => "EventSampleWorkflow";
            
        public int Version => 1;
            
        public void Build(IWorkflowBuilder<MyDataClass> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                .Parallel()
                    .Do(branch1 => branch1
                        .StartWith(context => Console.WriteLine("waiting in branch 1"))
                        .WaitForWithCancel("Event1", (data, context) => context.Workflow.Id, data => !string.IsNullOrEmpty(data.StrValue))
                            .Output(data => data.StrValue, step => step.EventData)
                        .Then(context => Console.WriteLine("continue on branch 1")))
                    .Do(branch2 => branch2
                        .StartWith(context => Console.WriteLine("waiting in branch 2"))
                        .WaitForWithCancel("Event2", (data, context) => context.Workflow.Id, data => !string.IsNullOrEmpty(data.StrValue))
                            .Output(data => data.StrValue, step => step.EventData)
                        .Then(context => Console.WriteLine("continue on branch 2")))
                    .Join()                        
                .Then<CustomMessage>()
                    .Input(step => step.Message, data => "The data from the event is " + data.StrValue)
                .Then(context => Console.WriteLine("workflow complete"));
        }
    }

    //public static class StepBuilderExtensions
    //{
    //    public static IStepBuilder<TData, WaitFor> WaitForCancellable(this IStepBuilder<TData, TStepBody> builder eventName, Expression<Func<TData, IStepExecutionContext, string>> eventKey, Expression<Func<TData, DateTime>> effectiveDate = null)
    //    {
    //        var newStep = new WorkflowStep<WaitFor>();
    //        WorkflowBuilder.AddStep(newStep);
    //        var stepBuilder = new StepBuilder<TData, WaitFor>(WorkflowBuilder, newStep);
    //        stepBuilder.Input((step) => step.EventName, (data) => eventName);
    //        stepBuilder.Input((step) => step.EventKey, eventKey);

    //        if (effectiveDate != null)
    //        {
    //            stepBuilder.Input((step) => step.EffectiveDate, effectiveDate);
    //        }

    //        Step.Outcomes.Add(new StepOutcome() { NextStep = newStep.Id });
    //        return stepBuilder;
    //    }
    //}
}
