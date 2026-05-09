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
    public class ForkInstanceScenario : WorkflowTest<ForkInstanceScenario.ForkInstanceWorkflow, ForkInstanceScenario.MyDataClass>
    {
        private const string EventName = "ForkInstanceScenario.Event";

        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;

        public class IncrementStep1 : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                var data = (MyDataClass)context.Workflow.Data;
                data.Result++;
                Interlocked.Increment(ref Step1Ticker);
                return ExecutionResult.Next();
            }
        }

        public class IncrementStep2 : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                var data = (MyDataClass)context.Workflow.Data;
                data.Result++;
                Interlocked.Increment(ref Step2Ticker);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string EventKey { get; set; }
            public bool Forked { get; set; }
            public int Result { get; set; }
        }

        public class ForkInstanceWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith<IncrementStep1>()
                    .WaitFor(EventName, data => data.EventKey, data => DateTime.Now)
                    .Then<IncrementStep2>();
            }
        }

        public ForkInstanceScenario()
        {
            Setup();
            Step1Ticker = 0;
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
            var workflowId = StartWorkflow(new MyDataClass { EventKey = eventKey, Forked = false });

            WaitForEventSubscription(EventName, eventKey, TimeSpan.FromSeconds(30));

            var forkId = Host.ForkWorkflow(workflowId, data => ((MyDataClass)data).Forked = true).Result;

            WaitForSubscriptionCount(EventName, eventKey, 2);

            Host.PublishEvent(EventName, eventKey, null).GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(forkId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(forkId).Should().Be(WorkflowStatus.Complete);
            GetData(workflowId).Forked.Should().BeFalse();
            GetData(forkId).Forked.Should().BeTrue();
            GetData(workflowId).Result.Should().Be(2);
            GetData(forkId).Result.Should().Be(2);
            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(2);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
