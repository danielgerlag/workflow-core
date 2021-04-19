using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class SagaScenario : WorkflowTest<SagaScenario.Workflow, SagaScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public bool ThrowException { get; set; }
        }

        public class Workflow : IWorkflow<MyDataClass>
        {
            public static bool Event1Fired = false;
            public static bool Event2Fired = false;
            public static bool Event3Fired = false;
            public static bool TailEventFired = false;
            public static bool Compensation1Fired = false;
            public static bool Compensation2Fired = false;
            public static bool Compensation3Fired = false;
            public static bool Compensation4Fired = false;
            public static bool Compensation5Fired = false;
            public static bool Compensation6Fired = false;

            public string Id => "SagaWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                        .CompensateWith(context => Compensation1Fired = true)
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                            .CompensateWith(context => Compensation2Fired = true)
                        .Then(context =>
                        {
                            Event1Fired = true;
                            if ((context.Workflow.Data as MyDataClass).ThrowException)
                                throw new Exception();
                            Event2Fired = true;
                        })
                            .CompensateWith(context => Compensation3Fired = true)
                        .Then(context => Event3Fired = true)
                            .CompensateWith(context => Compensation4Fired = true)
                        )
                        .CompensateWith(context => Compensation5Fired = true)
                    .Then(context => TailEventFired = true)
                        .CompensateWith(context => Compensation6Fired = true);
            }
        }

        public SagaScenario()
        {
            Setup();
            Workflow.Event1Fired = false;
            Workflow.Event2Fired = false;
            Workflow.Event3Fired = false;
            Workflow.Compensation1Fired = false;
            Workflow.Compensation2Fired = false;
            Workflow.Compensation3Fired = false;
            Workflow.Compensation4Fired = false;
            Workflow.Compensation5Fired = false;
            Workflow.Compensation6Fired = false;
            Workflow.TailEventFired = false;
        }

        [Fact]
        public void NoExceptionScenario()
        {            
            var workflowId = StartWorkflow(new MyDataClass { ThrowException = false });            
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);            
            Workflow.Event1Fired.Should().BeTrue();
            Workflow.Event2Fired.Should().BeTrue();
            Workflow.Event3Fired.Should().BeTrue();
            Workflow.Compensation1Fired.Should().BeFalse();
            Workflow.Compensation2Fired.Should().BeFalse();
            Workflow.Compensation3Fired.Should().BeFalse();
            Workflow.Compensation4Fired.Should().BeFalse();
            Workflow.Compensation5Fired.Should().BeFalse();
            Workflow.Compensation6Fired.Should().BeFalse();
            Workflow.TailEventFired.Should().BeTrue();
        }

        [Fact]
        public void ExceptionScenario()
        {
            var workflowId = StartWorkflow(new MyDataClass { ThrowException = true });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(1);
            Workflow.Event1Fired.Should().BeTrue();
            Workflow.Event2Fired.Should().BeFalse();
            Workflow.Event3Fired.Should().BeFalse();
            Workflow.Compensation1Fired.Should().BeFalse();
            Workflow.Compensation2Fired.Should().BeTrue();
            Workflow.Compensation3Fired.Should().BeTrue();
            Workflow.Compensation4Fired.Should().BeFalse();
            Workflow.Compensation5Fired.Should().BeTrue();
            Workflow.Compensation6Fired.Should().BeFalse();
            Workflow.TailEventFired.Should().BeTrue();
        }
    }
}
