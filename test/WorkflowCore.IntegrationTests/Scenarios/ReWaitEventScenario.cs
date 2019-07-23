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
    public class ReWaitEventScenario : WorkflowTest<ReWaitEventScenario.EventWorkflow, ReWaitEventScenario.MyIntClass>
    {
        public class MyIntClass
        {
            public int Value { get; set; }
        }

        public class EventWorkflow : IWorkflow<ReWaitEventScenario.MyIntClass>
        {
            public string Id => "EventWorkflow2";
            public int Version => 1;
            public void Build(IWorkflowBuilder<ReWaitEventScenario.MyIntClass> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        var data = (context.Workflow.Data as MyIntClass ?? new MyIntClass { Value = 0 });
                        data.Value++;

                        if (data.Value > 3)
                            return ExecutionResult.Next();

                        return ExecutionResult.ReWaitForEvent("MyEvent", data.Value + "", DateTime.Now);
                    });
            }
        }

        public ReWaitEventScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyIntClass { Value = 0 });
            WaitForEventSubscription("MyEvent", "1", TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", "1", null);

            WaitForEventSubscription("MyEvent", "2", TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", "2", null);

            WaitForEventSubscription("MyEvent", "3", TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", "3", null);

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetData(workflowId).Value.Should().Be(4);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
