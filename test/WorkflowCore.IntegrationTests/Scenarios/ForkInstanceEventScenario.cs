using System;
using System.Collections.Generic;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ForkInstanceEventScenario : WorkflowTest<ForkInstanceEventScenario.ForkInstanceEventWorkflow, ForkInstanceEventScenario.MyDataClass>
    {
        private const string Event1Name = "ForkInstanceEventScenario.Event1";
        private const string Event2Name = "ForkInstanceEventScenario.Event2";

        public class MyDataClass
        {
            public string EventKey1 { get; set; }
            public string EventKey2 { get; set; }
            public string StrValue1 { get; set; }
            public string StrValue2 { get; set; }
        }

        public class ForkInstanceEventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceEventWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor(Event1Name, data => data.EventKey1, data => DateTime.Now)
                        .Output(data => data.StrValue1, step => step.EventData)
                    .WaitFor(Event2Name, data => data.EventKey2, data => DateTime.Now)
                        .Output(data => data.StrValue2, step => step.EventData);
            }
        }

        public ForkInstanceEventScenario()
        {
            Setup();
        }

        private void WaitForSubscriptionCount(string eventName, string eventKey, int expectedCount)
        {
            var counter = 0;
            while ((new List<EventSubscription>(GetActiveSubscriptons(eventName, eventKey)).Count < expectedCount)
                && (counter < 300))
            {
                Thread.Sleep(100);
                counter++;
            }
        }

        [Fact]
        public void Scenario()
        {
            var eventKey1 = Guid.NewGuid().ToString();
            var eventKey2 = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { EventKey1 = eventKey1, EventKey2 = eventKey2 });

            WaitForEventSubscription(Event1Name, eventKey1, TimeSpan.FromSeconds(30));

            var forkId = Host.ForkWorkflow(workflowId).Result;

            WaitForSubscriptionCount(Event1Name, eventKey1, 2);

            Host.PublishEvent(Event1Name, eventKey1, "Pass1").GetAwaiter().GetResult();

            WaitForSubscriptionCount(Event2Name, eventKey2, 2);

            Host.PublishEvent(Event2Name, eventKey2, "Pass2").GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(forkId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(forkId).Should().Be(WorkflowStatus.Complete);
            GetData(workflowId).StrValue1.Should().Be("Pass1");
            GetData(workflowId).StrValue2.Should().Be("Pass2");
            GetData(forkId).StrValue1.Should().Be("Pass1");
            GetData(forkId).StrValue2.Should().Be("Pass2");
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
