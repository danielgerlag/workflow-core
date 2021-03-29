using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class EventOrderScenario : WorkflowTest<EventOrderScenario.EventWorkflow, EventOrderScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
            public int Value4 { get; set; }
            public int Value5 { get; set; }
        }

        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventOrder";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("OrderedEvent", data => string.Empty, data => new DateTime(2000, 1, 1, 0, 1, 0))
                        .Output(data => data.Value1, step => step.EventData)
                    .WaitFor("OrderedEvent", data => string.Empty, data => new DateTime(2000, 1, 1, 0, 2, 0))
                        .Output(data => data.Value2, step => step.EventData)
                    .WaitFor("OrderedEvent", data => string.Empty, data => new DateTime(2000, 1, 1, 0, 3, 0))
                        .Output(data => data.Value3, step => step.EventData)
                    .WaitFor("OrderedEvent", data => string.Empty, data => new DateTime(2000, 1, 1, 0, 4, 0))
                        .Output(data => data.Value4, step => step.EventData)
                    .WaitFor("OrderedEvent", data => string.Empty, data => new DateTime(2000, 1, 1, 0, 5, 0))
                        .Output(data => data.Value5, step => step.EventData);
            }
        }

        public EventOrderScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            Host.PublishEvent("OrderedEvent", string.Empty, 1, new DateTime(2000, 1, 1, 0, 1, 1));
            Host.PublishEvent("OrderedEvent", string.Empty, 2, new DateTime(2000, 1, 1, 0, 2, 1));
            Host.PublishEvent("OrderedEvent", string.Empty, 3, new DateTime(2000, 1, 1, 0, 3, 1));
            Host.PublishEvent("OrderedEvent", string.Empty, 4, new DateTime(2000, 1, 1, 0, 4, 1));
            Host.PublishEvent("OrderedEvent", string.Empty, 5, new DateTime(2000, 1, 1, 0, 5, 1));

            var workflowId = StartWorkflow(new MyDataClass());
            
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value1.Should().Be(1);
            GetData(workflowId).Value2.Should().Be(2);
            GetData(workflowId).Value3.Should().Be(3);
            GetData(workflowId).Value4.Should().Be(4);
            GetData(workflowId).Value5.Should().Be(5);
        }
    }
}
