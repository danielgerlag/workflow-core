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
    public class BasicWorkflowTest : WithFakes<MoqFakeEngine>
    {

        static int Step1Ticker = 0;
        static int Step2Ticker = 0;

        public class Step1 : StepBody
        {            
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step1Ticker++;
                return ExecutionResult.Next();
            }
        }        

        class BasicWorkflow : IWorkflow
        {
            public string Id => "BasicWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<Object> builder)
            {
                builder
                    .StartWith<Step1>()
                    .Then(context =>
                    {
                        Step2Ticker++;
                        return ExecutionResult.Next();
                    });
                        
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
            registry.RegisterWorkflow(new BasicWorkflow());

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.Start();            
        };

        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("BasicWorkflow").Result;
            Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;
            int counter = 0;
            while ((Instance.Status == WorkflowStatus.Runnable) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;
                Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;                
            }
        };

        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        It should_execute_step1_once = () => Step1Ticker.ShouldEqual(1);
        It should_execute_step2_once = () => Step2Ticker.ShouldEqual(1);

        Cleanup after = () =>
        {
            Host.Stop();
        };


    }
}
