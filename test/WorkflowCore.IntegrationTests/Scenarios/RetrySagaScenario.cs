using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class RetrySagaScenario : WorkflowTest<RetrySagaScenario.Workflow, RetrySagaScenario.MyDataClass>
    {
        public class MyDataClass
        {            
        }

        public class Workflow : IWorkflow<MyDataClass>
        {
            public static int Event1Fired;
            public static int Event2Fired;
            public static int Event3Fired;
            public static int TailEventFired;
            public static int Compensation1Fired;
            public static int Compensation2Fired;
            public static int Compensation3Fired;
            public static int Compensation4Fired;
            
            public string Id => "RetrySagaWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                        .CompensateWith(context => Compensation1Fired++)
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                            .CompensateWith(context => Compensation2Fired++)
                        .Then(context =>
                        {
                            Event1Fired++;
                            if (Event1Fired < 3)
                                throw new Exception();
                            Event2Fired++;
                        })
                            .CompensateWith(context => Compensation3Fired++)
                        .Then(context => Event3Fired++)
                            .CompensateWith(context => Compensation4Fired++)
                        )
                        .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(1))
                    .Then(context => TailEventFired++);
            }
        }

        public RetrySagaScenario()
        {
            Setup();
            Workflow.Event1Fired = 0;
            Workflow.Event2Fired = 0;
            Workflow.Event3Fired = 0;
            Workflow.Compensation1Fired = 0;
            Workflow.Compensation2Fired = 0;
            Workflow.Compensation3Fired = 0;
            Workflow.Compensation4Fired = 0;
            Workflow.TailEventFired = 0;
        }
                
        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass());
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(2);
            Workflow.Event1Fired.Should().Be(3);
            Workflow.Event2Fired.Should().Be(1);
            Workflow.Event3Fired.Should().Be(1);
            Workflow.Compensation1Fired.Should().Be(0);
            Workflow.Compensation2Fired.Should().Be(2);
            Workflow.Compensation3Fired.Should().Be(2);
            Workflow.Compensation4Fired.Should().Be(0);            
            Workflow.TailEventFired.Should().Be(1);
        }
    }
}
