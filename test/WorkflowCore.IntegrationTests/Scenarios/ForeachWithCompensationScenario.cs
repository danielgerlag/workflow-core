using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ForeachWithCompensationScenario : WorkflowTest<ForeachWithCompensationScenario.ForeachWorkflow, ForeachWithCompensationScenario.MyDataClass>
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;
        internal static int Step3Ticker = 0;
        internal static int CompensateTicker = 0;

        public class MyDataClass
        {
        }

        public class ForeachWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForeachWithCompensationWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                .StartWith(data => {
                    Step1Ticker++;
                })
                .Then(data => {
                    Step2Ticker++;
                })
                .ForEach(step => new List<int> { 1 })
                    .Do(then => then
                        .Decide(data => 1)
                            .Branch(1, builder.CreateBranch()
                                .StartWith(data => {
                                    Step3Ticker++;
                                    throw new Exception();
                                })
                                .CompensateWithSequence(builder => builder.StartWith(_ => {
                                    CompensateTicker++;
                                })))
                    );
            }
        }

        public ForeachWithCompensationScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(1);
            Step3Ticker.Should().Be(1);
            CompensateTicker.Should().Be(1);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(1);
        }
    }
}
