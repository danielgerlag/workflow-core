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
    public class StepExecution : WithFakes<MoqFakeEngine>
    {
        static int Step1StepTicker = 0;
        static int Step2StepTicker = 0;

        class StepExecutionTestWorkflow : IWorkflow
        {
            public string Id { get { return "StepExecutionTestWorkflow"; } }
            public int Version { get { return 1; } }            
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        Step1StepTicker++;
                        return ExecutionResult.Next();
                    })
                    .Then(context =>
                    {
                        Step2StepTicker++;
                        return ExecutionResult.Next();
                    });
            }            
        }


        static IWorkflowExecutor Subject;
        static IWorkflowHost Host;
        static IPersistenceProvider PersistenceProvider;        
        static IWorkflowRegistry Registry;
        static WorkflowOptions Options;
        static WorkflowInstance Instance;        

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
            var def = new StepExecutionTestWorkflow();
            Registry.RegisterWorkflow(def);

            Instance = new WorkflowInstance();
            Instance.WorkflowDefinitionId = def.Id;
            Instance.Version = def.Version;
            Instance.Status = WorkflowStatus.Runnable;
            Instance.NextExecution = 0;
            Instance.Id = "001";
            
            Instance.ExecutionPointers.Add(new ExecutionPointer()
            {
                Active = true,
                ConcurrentFork = 1,
                StepId = 0
            });            
        };

        Because of = () =>
        {
            Subject.Execute(Instance, PersistenceProvider, Options).Wait();
            Subject.Execute(Instance, PersistenceProvider, Options).Wait();
        };


        It should_run_step1_once = () => Step1StepTicker.ShouldEqual(1);
        It should_run_step2_once = () => Step2StepTicker.ShouldEqual(1);
        It should_be_persisted = () => PersistenceProvider.WasToldTo(x => x.PersistWorkflow(Instance));
        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        
        Cleanup after = () =>
        {
            
        };
        

    }
}
