using System;
using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class TryCatchScenario: WorkflowTest<TryCatchScenario.Workflow, TryCatchScenario.MyDataClass>
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
            public static bool CatchFired = false;

            public string Id => "TryCatchWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Try(b => b.StartWith(ctx => {
                            Event1Fired = true;
                            if (((MyDataClass) ctx.Workflow.Data).ThrowException)
                                throw new Exception();
                            Event2Fired = true;
                        })
                    )
                    .Catch(new[]{typeof(Exception)}, context => CatchFired = true)
                    .Then(context => TailEventFired = true);
            }
        }
        
        public TryCatchScenario()
        {
            Setup();
            Workflow.Event1Fired = false;
            Workflow.Event2Fired = false;
            Workflow.CatchFired = false;
            Workflow.TailEventFired = false;
        }
        
        [Fact]
        public void NoExceptionScenario()
        {            
            var workflowId = StartWorkflow(new MyDataClass() { ThrowException = false });            
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);            
            Workflow.Event1Fired.Should().BeTrue();
            Workflow.Event2Fired.Should().BeTrue();
            Workflow.CatchFired.Should().BeFalse();
            Workflow.TailEventFired.Should().BeTrue();
        }

        [Fact]
        public void ExceptionScenario()
        {
            var workflowId = StartWorkflow(new MyDataClass() { ThrowException = true });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(1);
            Workflow.Event1Fired.Should().BeTrue();
            Workflow.Event2Fired.Should().BeFalse();
            Workflow.CatchFired.Should().BeTrue();
            Workflow.TailEventFired.Should().BeTrue();
        }
    }
}