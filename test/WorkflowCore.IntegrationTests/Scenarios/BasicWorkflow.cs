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
    public class BasicWorkflow : BaseScenario<BasicWorkflow.WorkflowDef, object>
    {
        protected static string WorkflowId;
        protected static WorkflowInstance Instance;

        public class Step1 : StepBody
        {            
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step1Ticker++;
                return ExecutionResult.Next();
            }
        }        

        public class WorkflowDef : IWorkflow
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
        
        protected static int Step1Ticker = 0;
        protected static int Step2Ticker = 0;        

        Behaves_like<BasicWorkflowBehavior> a_basic_workflow;
        
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow();
        }

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

        protected override void CleanupAfter()
        {
            base.CleanupAfter();
            Step1Ticker = 0;
            Step2Ticker = 0;
        }        
    }
}

