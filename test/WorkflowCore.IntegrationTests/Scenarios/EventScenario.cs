using System;
using System.Collections.Generic;
using System.Text;
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
            public string StrValue { get; set; }
        }

        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue)
                        .Output(data => data.StrValue, step => step.EventData);
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
            var workflowId = StartWorkflow(new MyDataClass() { StrValue = eventKey });
            WaitForEventSubscription("MyEvent", eventKey, TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", eventKey, "Pass");
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).StrValue.Should().Be("Pass");
        }
    }
}
