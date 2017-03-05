using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.TestAssets.Workflows.HelloWorld;
using Machine.Fakes;
using Machine.Fakes.Adapters.Moq;
using WorkflowCore.Models;
using WorkflowCore.TestAssets.Workflows;

namespace WorkflowCore.UnitTests.WorkflowExecutorTests
{
    [Subject(typeof(WorkflowExecutor))]
    public class EventSubscribe : WithFakes<MoqFakeEngine>
    {
        static int StartStepTicker = 0;

        class EventSubscribeTestWorkflow : IWorkflow
        {
            public string Id { get { return "EventSubscribeTestWorkflow"; } }
            public int Version { get { return 1; } }            
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        StartStepTicker++;
                        return ExecutionResult.Next();
                    })
                    .WaitFor("MyEvent", data => "0");
            }
            
        }


        static IWorkflowExecutor Subject;
        static IWorkflowHost Host;
        static IPersistenceProvider PersistenceProvider;        
        static IWorkflowRegistry Registry;
        static WorkflowOptions Options;
        static WorkflowInstance Instance;
        static ExecutionPointer ExecutionPointer;

        Establish context = () =>
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();

            Options = new WorkflowOptions();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.AddTransient<IWorkflowRegistry, WorkflowRegistry>();
            

            Host = The<IWorkflowHost>();
            PersistenceProvider = The<IPersistenceProvider>();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);

            Registry = serviceProvider.GetService<IWorkflowRegistry>();

            Subject = new WorkflowExecutor(Host, Registry, serviceProvider, loggerFactory);
            var def = new EventSubscribeTestWorkflow();
            Registry.RegisterWorkflow(def);

            Instance = new WorkflowInstance();
            Instance.WorkflowDefinitionId = def.Id;
            Instance.Version = def.Version;
            Instance.Status = WorkflowStatus.Runnable;
            Instance.NextExecution = 0;
            Instance.Id = "001";

            ExecutionPointer = new ExecutionPointer()
            {
                Active = true,
                ConcurrentFork = 1,
                StepId = 1
            };

            Instance.ExecutionPointers.Add(ExecutionPointer);            
        };

        Because of = () => Subject.Execute(Instance, PersistenceProvider, Options).Wait();
                
        It should_have_an_event_name = () => ExecutionPointer.EventName.ShouldEqual("MyEvent");
        It should_have_an_event_key = () => ExecutionPointer.EventKey.ShouldEqual("0");
        It should_not_be_active = () => ExecutionPointer.Active.ShouldBeFalse();
        It should_be_persisted = () => PersistenceProvider.WasToldTo(x => x.PersistWorkflow(Instance));
        It should_create_a_subscription = () => Host.WasToldTo(x => x.SubscribeEvent(Instance.Id, ExecutionPointer.StepId, "MyEvent", "0"));

        Cleanup after = () =>
        {
            
        };
        

    }
}
