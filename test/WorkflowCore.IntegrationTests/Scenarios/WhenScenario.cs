using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Threading;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class WhenScenario : WorkflowTest<WhenScenario.WhenWorkflow, WhenScenario.MyDataClass>
    {
        internal static int Case1Ticker = 0;
        internal static int Case2Ticker = 0;
        internal static int Case3Ticker = 0;
        internal static DateTime LastBlock;
        internal static DateTime AfterBlock;

        public class MyDataClass
        {
            public int Counter { get; set; }
        }

        public class WhenWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "WhenWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Outcome(2))
                    .When(data => 1).Do(then => then
                        .StartWith(context =>
                        {
                            Case1Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .When(data => 2).Do(then => then
                        .StartWith(context =>
                        {
                            Case2Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .When(data => 2).Do(then => then
                        .StartWith(context =>
                        {
                            Case3Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .Then(context =>
                    {
                        AfterBlock = DateTime.Now;
                        return ExecutionResult.Next();
                    });
            }
        }

        public WhenScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass { Counter = 2 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            Case1Ticker.Should().Be(0);
            Case2Ticker.Should().Be(1);
            Case3Ticker.Should().Be(1);
            AfterBlock.Should().BeAfter(LastBlock);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}