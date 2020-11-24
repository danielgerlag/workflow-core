using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class MultistepCompensationScenario : WorkflowTest<MultistepCompensationScenario.Workflow, Object>
    {
        public class Workflow : IWorkflow
        {
            public static int Compensation1Fired = -1;
            public static int Compensation2Fired = -1;
            public static int Compensation3Fired = -1;
            public static int Compensation4Fired = -1;
            public static int CompensationCounter = 0;

            public string Id => "CompensatingWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                            .CompensateWith(context =>
                            {
                                CompensationCounter++;
                                Compensation1Fired = CompensationCounter;
                            })
                        .Then(context => ExecutionResult.Next())
                            .CompensateWithSequence(context => context.StartWith(c =>
                            {
                                CompensationCounter++;
                                Compensation2Fired = CompensationCounter;
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
                            })
                        );
            }
        }

        public MultistepCompensationScenario()
        {
            Setup();
            Workflow.Compensation1Fired = -1;
            Workflow.Compensation2Fired = -1;
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
            
            Workflow.Compensation1Fired.Should().Be(4);
            Workflow.Compensation2Fired.Should().Be(3);
            Workflow.Compensation3Fired.Should().Be(2);
            Workflow.Compensation4Fired.Should().Be(1);
        }
    }
}
