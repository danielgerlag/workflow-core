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
                    .WaitFor("MyEvent0", data => data.StrValue1, data => DateTime.Now)
                        .Output(data => data.StrValue1, step => step.EventData)
                    .WaitFor("MyEvent1", data => data.StrValue2)
                        .Output(data => data.StrValue2, step => step.EventData);
            }
        }

        public WorkflowPurgeScenario()
        {
            Setup();
        }

        public async Task ScenarioAsync()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { StrValue1 = eventKey, StrValue2 = eventKey });

            for(int i = 0; i < EventsPurger.BatchSize * 2; i++)
            {
                WaitForEventSubscription($"MyEvent{i}", eventKey, TimeSpan.FromSeconds(30));
                await Host.PublishEvent($"MyEvent{i}", eventKey, $"Pass{i}");
                GetEvents(eventKey, $"MyEvent{i}").Count().Should().Be(1);
            }

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).StrValue1.Should().Be("Pass0");
            GetData(workflowId).StrValue2.Should().Be("Pass1");

            await WorkflowPurger.PurgeWorkflows(WorkflowStatus.Complete, DateTime.UtcNow);
            await EventsPurger.PurgeEvents(DateTime.UtcNow);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => GetWorkflowInstance(workflowId));
            exception.Message.Should().Contain("Sequence contains no elements");
            for (int i = 0; i < EventsPurger.BatchSize * 2; i++)
            {
                GetEvents(eventKey, $"MyEvent{i}").Count().Should().Be(0);
            }
        }
    }
}
