using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class EventScenario : BaseScenario<EventScenario.EventWorkflow, EventScenario.MyDataClass>
    {
        public class MyDataClass
        {
            public string StrValue { get; set; }
        }

        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue)
                        .Output(data => data.StrValue, step => step.EventData);
            }
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("EventWorkflow", new MyDataClass() { StrValue = "0" }).Result;

            int counter = 0;
            while ((PersistenceProvider.GetSubcriptions("MyEvent", "0", DateTime.MaxValue).Result.Count() == 0) && (counter < 150))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
            }

            Host.PublishEvent("MyEvent", "0", "Pass");

            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 150))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            (instance.Data as MyDataClass).StrValue.Should().Be("Pass");
        }
    }
}
