using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ParallelScenario : WorkflowTest<ParallelScenario.ParallelWorkflow, ParallelScenario.MyDataClass>
    {
        internal static int StartStepTicker = 0;
        internal static int EndStepTicker = 0;

        internal static int Step11Ticker = 0;
        internal static int Step12Ticker = 0;
        internal static int Step21Ticker = 0;
        internal static int Step22Ticker = 0;
        internal static int Step31Ticker = 0;
        internal static int Step32Ticker = 0;

        public class MyDataClass
        {
        }

        public class ParallelWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ParallelWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(x => 
                    {
                        StartStepTicker++;
                        return ExecutionResult.Next();
                    })
                    .Parallel()
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step11Ticker++;
                            return ExecutionResult.Next();
                        })
                        .Then(x =>
                        {
                            Step12Ticker++;
                            return ExecutionResult.Next();
                        }))
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step21Ticker++;
                            return ExecutionResult.Next();
                        })
                        .WaitFor("MyEventInParallel", data => "0")
                        .Then(x =>
                        {
                            Step22Ticker++;
                            return ExecutionResult.Next();
                        }))
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step31Ticker++;
                            return ExecutionResult.Next();
                        })
                        .Then(x =>
                        {
                            Step32Ticker++;
                            return ExecutionResult.Next();
                        }))
                .Join()
                .Then(x =>
                {
                    EndStepTicker++;
                    return ExecutionResult.Next();
                });
            }
        }

        public ParallelScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass());

            var counter = 0;
            while ((Step12Ticker == 0) && (Step32Ticker == 0) && (counter < 150))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
            }

            WaitForEventSubscription("MyEventInParallel", "0", TimeSpan.FromSeconds(30));

            Step22Ticker.Should().Be(0);

            Host.PublishEvent("MyEventInParallel", "0", "Pass");

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            StartStepTicker.Should().Be(1);
            EndStepTicker.Should().Be(1);
            Step11Ticker.Should().Be(1);
            Step12Ticker.Should().Be(1);
            Step21Ticker.Should().Be(1);
            Step22Ticker.Should().Be(1);
            Step31Ticker.Should().Be(1);
            Step32Ticker.Should().Be(1);
        }
    }
}
