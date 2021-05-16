using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class DecisionScenario : WorkflowTest<DecisionScenario.DecisionWorkflow, DecisionScenario.MyDataClass>
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

        public class SubtractNumbers : StepBody
        {
            public int Input1 { get; set; }
            public int Input2 { get; set; }
            public int Output { get; set; }

            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Output = (Input1 - Input2);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public string Op { get; set; }
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
        }

        public class DecisionWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "DecisionWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                var addBranch = builder.CreateBranch()
                    .StartWith<AddNumbers>()
                        .Input(step => step.Input1, data => data.Value1)
                        .Input(step => step.Input2, data => data.Value2)
                        .Output(data => data.Value3, step => step.Output);

                var subtractBranch = builder.CreateBranch()
                    .StartWith<SubtractNumbers>()
                        .Input(step => step.Input1, data => data.Value1)
                        .Input(step => step.Input2, data => data.Value2)
                        .Output(data => data.Value3, step => step.Output);

                builder
                    .StartWith<Decide>()
                        .Input(step => step.Expression, data => data.Op)
                        .Branch("+", addBranch)
                        .Branch("-", subtractBranch);
            }
        }

        public DecisionScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario1()
        {
            var workflowId = StartWorkflow(new MyDataClass { Op = "+", Value1 = 2, Value2 = 3 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value3.Should().Be(5);
        }

        [Fact]
        public void Scenario2()
        {
            var workflowId = StartWorkflow(new MyDataClass { Op = "-", Value1 = 2, Value2 = 3 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value3.Should().Be(-1);
        }
    }
}
