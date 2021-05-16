using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class BasicWorkflow : IWorkflow
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;

        public string Id => "BasicWorkflow";
        public int Version => 1;
        public void Build(IWorkflowBuilder<Object> builder)
        {
            builder
                .StartWith<Step1>()
                .Then(context =>
                {
                    Step2Ticker++;
                    return ExecutionResult.Next();
                });

        }
    }

    internal class Step1 : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            BasicWorkflow.Step1Ticker++;
            return ExecutionResult.Next();
        }
    }

    public class BasicScenario : WorkflowTest<BasicWorkflow, Object>
    {   
        public BasicScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            BasicWorkflow.Step1Ticker = 0;
            BasicWorkflow.Step2Ticker = 0;

            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            BasicWorkflow.Step1Ticker.Should().Be(1);
            BasicWorkflow.Step2Ticker.Should().Be(1);
        }
    }
}
