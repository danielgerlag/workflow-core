using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class CancelledEventScenario : WorkflowTest<CancelledEventScenario.EventWorkflow, CancelledEventScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public string StrValue { get; set; }
        }

        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public static bool Event1Fired = false;
            public static bool Event2Fired = false;

            public string Id => "CancelledEventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Parallel()
                        .Do(branch1 => branch1
                            .StartWith(context => ExecutionResult.Next())
                            .WaitFor("Event1", (data, context) => context.Workflow.Id, null, data => !string.IsNullOrEmpty(data.StrValue))
                                .Output(data => data.StrValue, step => step.EventData)
                            .Then(context => Event1Fired = true))
                        .Do(branch2 => branch2
                            .StartWith(context => ExecutionResult.Next())
                            .WaitFor("Event2", (data, context) => context.Workflow.Id, null, data => !string.IsNullOrEmpty(data.StrValue))
                                .Output(data => data.StrValue, step => step.EventData)
                            .Then(context => Event2Fired = true))
                        .Join()
                        .WaitFor("Event3", (data, context) => context.Workflow.Id, null);
            }
        }

        public CancelledEventScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {            
            var workflowId = StartWorkflow(new MyDataClass());
            WaitForEventSubscription("Event1", workflowId, TimeSpan.FromSeconds(30));
            WaitForEventSubscription("Event2", workflowId, TimeSpan.FromSeconds(30));
            Host.PublishEvent("Event2", workflowId, "Pass");
            WaitForEventSubscription("Event3", workflowId, TimeSpan.FromSeconds(30));
            Host.PublishEvent("Event1", workflowId, "Fail");
            Host.PublishEvent("Event3", workflowId, null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).StrValue.Should().Be("Pass");
            EventWorkflow.Event1Fired.Should().BeFalse();
            EventWorkflow.Event2Fired.Should().BeTrue();
        }
    }
}
