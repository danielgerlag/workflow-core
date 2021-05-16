using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class FailingSagaScenario : WorkflowTest<FailingSagaScenario.Workflow, object>
    {
        public class Workflow : IWorkflow<object>
        {
            public static int Event1Fired;
            public static int Event2Fired;
            public static int Event3Fired;
            
            public string Id => "NestedRetrySaga2Workflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                        .If(data => true)
                            .Do(i => i
                                .StartWith(context =>
                                {
                                    Event1Fired++;
                                    throw new Exception();
                                })
                            )
                        .Then(context => Event2Fired++)
                        )
                        .OnError(WorkflowErrorHandling.Terminate)
                    .Then(context => Event3Fired++);
            }
        }

        public FailingSagaScenario()
        {
            Setup();
            Workflow.Event1Fired = 0;
            Workflow.Event2Fired = 0;
            Workflow.Event3Fired = 0;
        }
                
        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Terminated);
            UnhandledStepErrors.Count.Should().Be(1);
            Workflow.Event1Fired.Should().Be(1);
            Workflow.Event2Fired.Should().Be(0);
            Workflow.Event3Fired.Should().Be(0);
        }
    }
}
