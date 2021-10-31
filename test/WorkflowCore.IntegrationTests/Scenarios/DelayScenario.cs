using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class DelayWorkflow : IWorkflow<DelayWorkflow.MyDataClass>
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;

        public string Id => "DelayWorkflow";
        public int Version => 1;
        public void Build(IWorkflowBuilder<DelayWorkflow.MyDataClass> builder)
        {
            builder
                .StartWith(context => Step1Ticker++)
                .Delay(data => data.WaitTime)
                .Then(context => Step2Ticker++);

        }

        public class MyDataClass
        {
            public TimeSpan WaitTime { get; set; }
        }
    }

    public class DelayScenario : WorkflowTest<DelayWorkflow, DelayWorkflow.MyDataClass>
    {   
        public DelayScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            DelayWorkflow.Step1Ticker = 0;
            DelayWorkflow.Step2Ticker = 0;
            
            var workflowId = StartWorkflow(new DelayWorkflow.MyDataClass()
            {
                WaitTime = Host.Options.PollInterval.Add(TimeSpan.FromSeconds(1))
            });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            DelayWorkflow.Step1Ticker.Should().Be(1);
            DelayWorkflow.Step2Ticker.Should().Be(1);
        }
    }
}
