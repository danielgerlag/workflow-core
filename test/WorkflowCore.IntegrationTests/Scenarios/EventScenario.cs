using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class EventScenario : WorkflowTest<EventScenario.EventWorkflow, EventScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public string StrValue1 { get; set; }
            public string StrValue2 { get; set; }
        }

        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue1, data => DateTime.Now)
                        .Output(data => data.StrValue1, step => step.EventData)
                    .WaitFor("MyEvent2", data => data.StrValue2)
                        .Output(data => data.StrValue2, step => step.EventData);
            }
        }

        public EventScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { StrValue1 = eventKey, StrValue2 = eventKey });
            WaitForEventSubscription("MyEvent", eventKey, TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", eventKey, "Pass1");
            WaitForEventSubscription("MyEvent2", eventKey, TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent2", eventKey, "Pass2");

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).StrValue1.Should().Be("Pass1");
            GetData(workflowId).StrValue2.Should().Be("Pass2");
        }
    }
}
