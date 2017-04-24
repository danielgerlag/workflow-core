using Machine.Fakes;
using Machine.Fakes.Adapters.Moq;
using Machine.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.IntegrationTests.Scenarios
{    
    [Behaviors]
    public class ExternalEventsBehavior
    {
        protected static string WorkflowId;
        protected static IPersistenceProvider PersistenceProvider;
        protected static WorkflowInstance Instance;

        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        It should_have_a_return_value_of_pass = () => (Instance.Data as ExternalEventsTest.DataClass).StrValue.ShouldEqual("Pass");
    }

    [Subject(typeof(WorkflowHost))]
    public class ExternalEventsTest : BaseScenario<ExternalEventsTest.EventWorkflow, ExternalEventsTest.DataClass>
    {
        protected static string WorkflowId;
        protected static WorkflowInstance Instance;

        public class DataClass
        {
            public string StrValue { get; set; }
        }

        public class EventWorkflow : IWorkflow<DataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<DataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue)
                        .Output(data => data.StrValue, step => step.EventData);
            }
        }
        
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow();
        }
        
        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("EventWorkflow", new DataClass() { StrValue = "0" }).Result;

            int counter = 0;
            while ((PersistenceProvider.GetSubcriptions("MyEvent", "0", DateTime.MaxValue).Result.Count() == 0) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;                
            }

            Host.PublishEvent("MyEvent", "1", "Fail");
            Host.PublishEvent("MyEvent", "0", "Pass");

            Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;
            counter = 0;
            while ((Instance.Status == WorkflowStatus.Runnable) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;
                Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;                
            }
        };

        Behaves_like<ExternalEventsBehavior> events_workflow;
        
    }
}
