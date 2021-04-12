using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class CompensationScenario2 : WorkflowTest<CompensationScenario2.Workflow, CompensationScenario2.MyDataClass>
    {
        public class MyDataClass
        {
            public bool ThrowException { get; set; }
        }

        public class Workflow : IWorkflow<MyDataClass>
        {
            public static bool Event1Fired = false;
            public static bool Event2Fired = false;
            public static bool TailEventFired = false;
            public static bool Compensation1Fired = false;
            public static bool Compensation2Fired = false;

            public string Id => "CompensationWorkflow2";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Then(context =>
                    {
                        Event1Fired = true;
                        if ((context.Workflow.Data as MyDataClass).ThrowException)
                            throw new Exception();
                        Event2Fired = true;
                    })
                    .CompensateWithSequence(seq => seq 
                        .StartWith(context => Compensation1Fired = true)
                        .Then(context => Compensation2Fired = true)
                    )
                    .Then(context => TailEventFired = true);
            }
        }

        public CompensationScenario2()
        {
            Setup();
            Workflow.Event1Fired = false;
            Workflow.Event2Fired = false;
            Workflow.Compensation1Fired = false;
            Workflow.Compensation2Fired = false;
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
            Workflow.Compensation1Fired.Should().BeFalse();
            Workflow.Compensation2Fired.Should().BeFalse();
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
            Workflow.Compensation1Fired.Should().BeTrue();
            Workflow.Compensation2Fired.Should().BeTrue();
            Workflow.TailEventFired.Should().BeTrue();
        }
    }
}
