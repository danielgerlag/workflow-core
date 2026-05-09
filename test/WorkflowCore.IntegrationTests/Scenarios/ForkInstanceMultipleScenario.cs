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
    public class ForkInstanceMultipleScenario : WorkflowTest<ForkInstanceMultipleScenario.ForkInstanceMultipleWorkflow, ForkInstanceMultipleScenario.MyDataClass>
    {
        private const string EventName = "ForkInstanceMultipleScenario.Event";

        internal static int Step2Ticker = 0;

        public class Step2 : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Interlocked.Increment(ref Step2Ticker);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string EventKey { get; set; }
        }

        public class ForkInstanceMultipleWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceMultipleWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor(EventName, data => data.EventKey, data => DateTime.Now)
                    .Then<Step2>();
            }
        }

        public ForkInstanceMultipleScenario()
        {
            Setup();
            Step2Ticker = 0;
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
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { EventKey = eventKey });

            WaitForEventSubscription(EventName, eventKey, TimeSpan.FromSeconds(30));

            var fork1Id = Host.ForkWorkflow(workflowId).Result;
            var fork2Id = Host.ForkWorkflow(workflowId).Result;

            WaitForSubscriptionCount(EventName, eventKey, 3);

            Host.PublishEvent(EventName, eventKey, null).GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(fork1Id, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(fork2Id, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(fork1Id).Should().Be(WorkflowStatus.Complete);
            GetStatus(fork2Id).Should().Be(WorkflowStatus.Complete);
            Step2Ticker.Should().Be(3);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
