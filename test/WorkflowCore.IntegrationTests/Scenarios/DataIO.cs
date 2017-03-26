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
    public class DataIOBehavior
    {
        protected static string WorkflowId;
        protected static IPersistenceProvider PersistenceProvider;
        protected static WorkflowInstance Instance;

        It should_be_marked_as_complete = () => Instance.Status.ShouldEqual(WorkflowStatus.Complete);
        It should_have_a_return_value_of_5 = () => (Instance.Data as DataIO.DataClass).Value3.ShouldEqual(5);
    }

    [Subject(typeof(WorkflowHost))]
    public class DataIO : BaseScenario<DataIO.WorkflowDef, DataIO.DataClass>
    {
        protected static string WorkflowId;
        protected static WorkflowInstance Instance;

        public class DataClass
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
        }

        class AddNumbers : StepBody
        {
            public int Input1 { get; set; }
            public int Input2 { get; set; }
            public int Output { get; set; }

            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Output = (Input1 + Input2);
                return ExecutionResult.Next();
            }
        }
                
        public class WorkflowDef : IWorkflow<DataClass>
        {
            public string Id => "DataIOWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<DataClass> builder)
            {
                builder
                    .StartWith<AddNumbers>()
                        .Input(step => step.Input1, data => data.Value1)
                        .Input(step => step.Input2, data => data.Value2)
                        .Output(data => data.Value3, step => step.Output);
            }
        }
                
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow();
        }
        
        Because of = () =>
        {
            WorkflowId = Host.StartWorkflow("DataIOWorkflow", new DataIO.DataClass() { Value1 = 2, Value2 = 3 }).Result;
            Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;
            int counter = 0;
            while ((Instance.Status == WorkflowStatus.Runnable) && (counter < 60))
            {
                System.Threading.Thread.Sleep(500);
                counter++;
                Instance = PersistenceProvider.GetWorkflowInstance(WorkflowId).Result;                
            }
        };

        Behaves_like<DataIOBehavior> a_data_io_workflow;
        
    }
}
