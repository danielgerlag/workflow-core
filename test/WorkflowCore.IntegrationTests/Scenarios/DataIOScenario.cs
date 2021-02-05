using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class DataIOScenario : WorkflowTest<DataIOScenario.DataIOWorkflow, DataIOScenario.MyDataClass>
    {
        public class AddNumbers : StepBody
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

        public class MyDataClass
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
            public decimal Value4 { get; set; }
            
            public DataSubclass SubValue { get; set; }
        }

        public class DataSubclass
        {
            public decimal Value5 { get; set; }
        }

        public class DataIOWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "DataIOWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith<AddNumbers>()
                        .Input(step => step.Input1, data => data.Value1)
                        .Input(step => step.Input2, data => data.Value2)
                        .Output(data => data.Value3, step => step.Output);
            }
        }

        public DataIOScenario()
        {
            Setup(true);
        }

        [Fact]
        public void Scenario()
        {
            decimal v4 = 1.235465673450897890m;
            var workflowId = StartWorkflow(new MyDataClass() {Value1 = 2, Value2 = 3, Value4 = v4, SubValue = new DataSubclass() {Value5 = v4}});
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value3.Should().Be(5);
            GetData(workflowId).Value4.Should().Be(v4);
        }
    }
}
