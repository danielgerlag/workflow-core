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
    public class ForkInstanceDataIsolationScenario : WorkflowTest<ForkInstanceDataIsolationScenario.ForkInstanceDataIsolationWorkflow, ForkInstanceDataIsolationScenario.MyDataClass>
    {
        private const string EventName = "ForkInstanceDataIsolationScenario.Event";

        public class SetInitialValue : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                ((MyDataClass)context.Workflow.Data).Value = 10;
                return ExecutionResult.Next();
            }
        }

        public class DoubleValue : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                var data = (MyDataClass)context.Workflow.Data;
                data.Value *= 2;
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string EventKey { get; set; }
            public int Value { get; set; }
        }

        public class ForkInstanceDataIsolationWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceDataIsolationWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith<SetInitialValue>()
                    .WaitFor(EventName, data => data.EventKey, data => DateTime.Now)
                    .Then<DoubleValue>();
            }
        }

        public ForkInstanceDataIsolationScenario()
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
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { EventKey = eventKey });

            WaitForEventSubscription(EventName, eventKey, TimeSpan.FromSeconds(30));

            var forkId = Host.ForkWorkflow(workflowId, data => ((MyDataClass)data).Value = 100).Result;

            WaitForSubscriptionCount(EventName, eventKey, 2);

            Host.PublishEvent(EventName, eventKey, null).GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(forkId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(forkId).Should().Be(WorkflowStatus.Complete);
            GetData(workflowId).Value.Should().Be(20);
            GetData(forkId).Value.Should().Be(200);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
