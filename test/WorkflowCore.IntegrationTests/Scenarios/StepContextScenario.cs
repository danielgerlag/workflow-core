using FluentAssertions;
using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class StepContextScenario : WorkflowTest<StepContextScenario.ForeachWorkflow, StepContextScenario.WorkflowData>
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

        public class LoopIteration
        {
            public int ToAdd { get; set; }
            public int Result { get; set; } = 0;
        }

        public class WorkflowData
        {
            public int Sum { get; set; } = 0;
            public int GreaterThree { get; set; } = 0;
            public List<LoopIteration> Iterations { get; set; }
        }

        public class ForeachWorkflow : IWorkflow<WorkflowData>
        {
            public string Id => "ForeachWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<WorkflowData> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        return ExecutionResult.Next();
                    })
                    .ForEach(x => x.Iterations)
                        .Do(
                            x => x.StartWith<AddNumbers>()
                                .Input(step => step.Input1, data => data.Sum)
                                .Input(step => step.Input2, (data, ctx) => ((LoopIteration)ctx.Item).ToAdd)
                                .Output((data, ctx) => ((LoopIteration)ctx.Item).Result, step => step.Output)
                                .Output(data => data.Sum, step => step.Output)
                            .If((step, ctx) => ((LoopIteration)ctx.Item).ToAdd > 3)
                                .Do(y => y.StartWith<AddNumbers>()
                                    .Input(step => step.Input1, data => data.GreaterThree)
                                    .Input(step => step.Input2, data => 1)
                                    .Output(data => data.GreaterThree, step => step.Output)
                        ))
                    .Then(context =>
                    {
                        return ExecutionResult.Next();
                    });
            }
        }

        public StepContextScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var data = new WorkflowData()
            {
                Iterations = new List<LoopIteration>(new[]
                {
                    new LoopIteration() {
                        ToAdd=3
                    },
                    new LoopIteration() {
                        ToAdd=4
                    },
                    new LoopIteration() {
                        ToAdd=5
                    }
                })
            };

            var workflowId = StartWorkflow(data);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            var result = GetData(workflowId);
            result.Sum.Should().Be(3 + 4 + 5);
            result.Iterations[0].Result.Should().Be(3);
            result.Iterations[1].Result.Should().Be(3 + 4);
            result.Iterations[2].Result.Should().Be(3 + 4 + 5);

            result.GreaterThree.Should().Be(2);

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
