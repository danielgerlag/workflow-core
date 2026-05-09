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
    public class ForkInstanceParallelScenario : WorkflowTest<ForkInstanceParallelScenario.ForkInstanceParallelWorkflow, ForkInstanceParallelScenario.MyDataClass>
    {
        private const string EventName = "ForkInstanceParallelScenario.Event";

        internal static int CounterA = 0;
        internal static int CounterB = 0;
        internal static int Step3Ticker = 0;

        public class BranchA : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Interlocked.Increment(ref CounterA);
                return ExecutionResult.Next();
            }
        }

        public class BranchB : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Interlocked.Increment(ref CounterB);
                return ExecutionResult.Next();
            }
        }

        public class Step3 : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Interlocked.Increment(ref Step3Ticker);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string EventKey { get; set; }
        }

        public class ForkInstanceParallelWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceParallelWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor(EventName, data => data.EventKey, data => DateTime.Now)
                    .Parallel()
                        .Do(then => then.StartWith<BranchA>())
                        .Do(then => then.StartWith<BranchB>())
                    .Join()
                    .Then<Step3>();
            }
        }

        public ForkInstanceParallelScenario()
        {
            Setup();
            CounterA = 0;
            CounterB = 0;
            Step3Ticker = 0;
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

            var forkId = Host.ForkWorkflow(workflowId).Result;

            WaitForSubscriptionCount(EventName, eventKey, 2);

            Host.PublishEvent(EventName, eventKey, null).GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(forkId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(forkId).Should().Be(WorkflowStatus.Complete);
            CounterA.Should().Be(2);
            CounterB.Should().Be(2);
            Step3Ticker.Should().Be(2);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
