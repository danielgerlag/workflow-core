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
    public class BasicWorkflowBehavior
    {
        protected static int Step1Ticker = 0;
        protected static int Step2Ticker = 0;        
        protected static string WorkflowId;        
        protected static WorkflowInstance Instance;

        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        It should_execute_step1_once = () => Step1Ticker.ShouldEqual(1);
        It should_execute_step2_once = () => Step2Ticker.ShouldEqual(1);
    }

    [Subject(typeof(WorkflowHost))]
    public class BasicWorkflow : WithFakes<MoqFakeEngine>
    {
        public class Step1 : StepBody
        {            
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step1Ticker++;
                return ExecutionResult.Next();
            }
        }        

        class BasicWorkflowDef : IWorkflow
        {
            public string Id { get { return "BasicWorkflow"; } }
            public int Version { get { return 1; } }
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

        protected Establish context;
        protected Cleanup after;
        protected Because of;

        protected static int Step1Ticker = 0;
        protected static int Step2Ticker = 0;
        protected static IWorkflowHost Host;
        protected static string WorkflowId;
        protected static IPersistenceProvider PersistenceProvider;
        protected static WorkflowInstance Instance;

        Behaves_like<BasicWorkflowBehavior> a_basic_workflow;

        public BasicWorkflow()
        {
            context = EstablishContext;
            of = BecauseOf;
            after = CleanupAfter;
        }

        protected virtual void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow();
        }

        void EstablishContext()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            ConfigureWorkflow(services);

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);

            var registry = serviceProvider.GetService<IWorkflowRegistry>();
            registry.RegisterWorkflow(new BasicWorkflowDef());

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.Start();
        }               

        void BecauseOf()
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
        }               

        void CleanupAfter()
        {
            Host.Stop();
            Step1Ticker = 0;
            Step2Ticker = 0;
            Host = null;
            WorkflowId = null;
            Instance = null;
            PersistenceProvider = null;
        }
    }
}

