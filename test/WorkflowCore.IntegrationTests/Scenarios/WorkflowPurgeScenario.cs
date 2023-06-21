using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class WorkflowPurgeScenario : WorkflowTest<WorkflowPurgeScenario.WorkflowPurgeWorkflow, WorkflowPurgeScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public string StrValue1 { get; set; }
            public string StrValue2 { get; set; }
        }

        public class WorkflowPurgeWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "PurgeWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue1, data => DateTime.Now)
                        .Output(data => data.StrValue1, step => step.EventData)
                    .WaitFor("MyEvent2", data => data.StrValue2)
                        .Output(data => data.StrValue2, step => step.EventData);
            }
        }

        public WorkflowPurgeScenario()
        {
            Setup();
        }

        [Fact]
        public async Task ScenarioAsync()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { StrValue1 = eventKey, StrValue2 = eventKey });
            WaitForEventSubscription("MyEvent", eventKey, TimeSpan.FromSeconds(30));
            await Host.PublishEvent("MyEvent", eventKey, "Pass1");
            WaitForEventSubscription("MyEvent2", eventKey, TimeSpan.FromSeconds(30));
            await Host.PublishEvent("MyEvent2", eventKey, "Pass2");

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).StrValue1.Should().Be("Pass1");
            GetData(workflowId).StrValue2.Should().Be("Pass2");

            GetEvents(eventKey, "MyEvent").Count().Should().Be(1);
            GetEvents(eventKey, "MyEvent2").Count().Should().Be(1);

            await WorkflowPurger.PurgeWorkflows(WorkflowStatus.Complete, DateTime.UtcNow);
            await EventsPurger.PurgeEvents(DateTime.UtcNow);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => GetWorkflowInstance(workflowId));
            exception.Message.Should().Contain("Sequence contains no elements");
            GetEvents(eventKey, "MyEvent").Count().Should().Be(0);
            GetEvents(eventKey, "MyEvent2").Count().Should().Be(0);
        }
    }
}
