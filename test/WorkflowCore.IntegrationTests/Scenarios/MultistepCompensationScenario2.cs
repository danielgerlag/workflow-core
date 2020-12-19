using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class MultistepCompensationScenario2 : WorkflowTest<MultistepCompensationScenario2.Workflow, Object>
    {
        public class Workflow : IWorkflow
        {
            public static int CompensationCounter = 0;
            public static int Compensation1_1Fired = 0;
            public static int Compensation1_2Fired = 0;
            public static int Compensation2_1Fired = 0;
            public static int Compensation2_2Fired = 0;
            public static int Compensation3Fired = 0;
            public static int Compensation4Fired = 0;


            public string Id => "MultistepCompensationScenario2";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                            .CompensateWithSequence(context => context.StartWith(c =>
                            {
                                CompensationCounter++;
                                Compensation1_1Fired = CompensationCounter;
                            })
                            .Then(c =>
                            {
                                CompensationCounter++;
                                Compensation1_2Fired = CompensationCounter;
                            }))
                        .If(c => true).Do(then => then
                            .Then(context => ExecutionResult.Next())
                                .CompensateWithSequence(context => context.StartWith(c =>
                                {
                                    CompensationCounter++;
                                    Compensation2_1Fired = CompensationCounter;
                                }).Then(c =>
                                {
                                    CompensationCounter++;
                                    Compensation2_2Fired = CompensationCounter;
                                }))
                            .Then(context => ExecutionResult.Next())
                                .CompensateWith(context =>
                                {
                                    CompensationCounter++;
                                    Compensation3Fired = CompensationCounter;
                                })
                            .Then(context => throw new Exception())
                                .CompensateWith(context =>
                                {
                                    CompensationCounter++;
                                    Compensation4Fired = CompensationCounter;
                                }))
                    );
            }
        }

        public MultistepCompensationScenario2()
        {
            Setup();
            Workflow.Compensation1_1Fired = -1;
            Workflow.Compensation1_2Fired = -1;
            Workflow.Compensation2_1Fired = -1;
            Workflow.Compensation2_2Fired = -1;
            Workflow.Compensation3Fired = -1;
            Workflow.Compensation4Fired = -1;
            Workflow.CompensationCounter = 0;
        }
        
        [Fact]
        public void MultiCompensationStepOrder()
        {
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(1);

            Workflow.Compensation1_2Fired.Should().Be(6);
            Workflow.Compensation1_1Fired.Should().Be(5);
            Workflow.Compensation2_2Fired.Should().Be(4);
            Workflow.Compensation2_1Fired.Should().Be(3);
            Workflow.Compensation3Fired.Should().Be(2);
            Workflow.Compensation4Fired.Should().Be(1);
        }
    }
}
