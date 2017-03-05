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
    [Subject(typeof(WorkflowHost))]
    public class ExternalEventsTest : WithFakes<MoqFakeEngine>
    {

        public class MyDataClass
        {
            public string StrValue { get; set; }
        }

        class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id { get { return "EventWorkflow"; } }
            public int Version { get { return 1; } }
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.StrValue)
                        .Output(data => data.StrValue, step => step.EventData);
            }
        }
                        
        static IWorkflowHost Host;
        static string WorkflowId;
        static IPersistenceProvider PersistenceProvider;
        static WorkflowInstance Instance;


        Establish context = () =>
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);

            var registry = serviceProvider.GetService<IWorkflowRegistry>();            
            registry.RegisterWorkflow(new EventWorkflow());

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.Start();            
        };

        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("EventWorkflow", new MyDataClass() { StrValue = "0" }).Result;

            int counter = 0;
            while ((PersistenceProvider.GetSubcriptions("MyEvent", "0").Result.Count() == 0) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;                
            }

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

        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        It should_have_a_return_value_of_pass = () => (Instance.Data as MyDataClass).StrValue.ShouldEqual("Pass");

        Cleanup after = () =>
        {
            Host.Stop();
        };


    }
}
