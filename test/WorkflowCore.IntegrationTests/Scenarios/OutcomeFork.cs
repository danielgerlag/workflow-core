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
    public class OutcomeForkTest : WithFakes<MoqFakeEngine>
    {

        static int TaskATicker = 0;
        static int TaskBTicker = 0;
        static int TaskCTicker = 0;

        public class TaskA : StepBody
        {            
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                TaskATicker++;
                return ExecutionResult.Outcome(true);
            }
        }

        public class TaskB : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                TaskBTicker++;
                return ExecutionResult.Next();
            }
        }

        public class TaskC : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                TaskCTicker++;
                return ExecutionResult.Next();
            }
        }

        class OutcomeFork : IWorkflow
        {
            public string Id { get { return "OutcomeFork"; } }
            public int Version { get { return 1; } }
            public void Build(IWorkflowBuilder<Object> builder)
            {
                var taskA = builder.StartWith<TaskA>();
                taskA
                    .When(false)
                    .Then<TaskB>();
                taskA
                    .When(true)
                    .Then<TaskC>();
                    
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
            registry.RegisterWorkflow(new OutcomeFork());

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.Start();            
        };

        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("OutcomeFork").Result;
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
        It should_execute_taskA_once = () => TaskATicker.ShouldEqual(1);
        It should_not_execute_taskB = () => TaskBTicker.ShouldEqual(0);
        It should_execute_taskC_once = () => TaskCTicker.ShouldEqual(1);

        Cleanup after = () =>
        {
            Host.Stop();
        };


    }
}
