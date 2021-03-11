using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class AttachScenario : WorkflowTest<AttachScenario.GotoWorkflow, AttachScenario.MyDataClass>
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;

        public class MyDataClass
        {
        }

        public class GotoWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "GotoWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        Step1Ticker++;
                        return ExecutionResult.Next();
                    })
                        .Id("step1")
                    .If(data => Step1Ticker < 4).Do(then => then
                        .StartWith(context =>
                        {
                            Step2Ticker++;
                            return ExecutionResult.Next();
                        })
                        .Attach("step1")
                    );
            }
        }

        public AttachScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            Step1Ticker.Should().Be(4);
            Step2Ticker.Should().Be(3);

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
