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
    public class ForkInstanceForeachScenario : WorkflowTest<ForkInstanceForeachScenario.ForkInstanceForeachWorkflow, ForkInstanceForeachScenario.MyDataClass>
    {
        private const string EventName = "ForkInstanceForeachScenario.Event";

        internal static int ProcessItemTicker = 0;
        internal static int Step3Ticker = 0;

        public class ProcessItem : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                var data = (MyDataClass)context.Workflow.Data;
                data.ProcessedCount++;
                Interlocked.Increment(ref ProcessItemTicker);
                return ExecutionResult.Next();
            }
        }

        public class RecordFinalCount : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                var data = (MyDataClass)context.Workflow.Data;
                data.FinalCount = data.ProcessedCount;
                Interlocked.Increment(ref Step3Ticker);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string EventKey { get; set; }
            public List<int> Items { get; set; } = new List<int>();
            public int ProcessedCount { get; set; }
            public int FinalCount { get; set; }
        }

        public class ForkInstanceForeachWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForkInstanceForeachWorkflow";
            public int Version => 1;

            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor(EventName, data => data.EventKey, data => DateTime.Now)
                    .ForEach(data => data.Items, _ => false)
                        .Do(then => then.StartWith<ProcessItem>())
                    .Then<RecordFinalCount>();
            }
        }

        public ForkInstanceForeachScenario()
        {
            Setup();
            ProcessItemTicker = 0;
            Step3Ticker = 0;
        }

        [Fact]
        public void Scenario()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { EventKey = eventKey, Items = new List<int> { 1, 2, 3 } });

            WaitForEventSubscription(EventName, eventKey, TimeSpan.FromSeconds(30));

            var forkId = Host.ForkWorkflow(workflowId, data => ((MyDataClass)data).Items = new List<int> { 4, 5 }).Result;

            Host.PublishEvent(EventName, eventKey, null).GetAwaiter().GetResult();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(forkId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetStatus(forkId).Should().Be(WorkflowStatus.Complete);
            GetData(workflowId).ProcessedCount.Should().Be(3);
            GetData(workflowId).FinalCount.Should().Be(3);
            GetData(forkId).ProcessedCount.Should().Be(2);
            GetData(forkId).FinalCount.Should().Be(2);
            ProcessItemTicker.Should().Be(5);
            Step3Ticker.Should().Be(2);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
