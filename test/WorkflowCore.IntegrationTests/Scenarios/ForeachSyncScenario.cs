using FluentAssertions;
using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ForeachSyncScenario : WorkflowTest<ForeachSyncScenario.ForeachSyncWorkflow, ForeachSyncScenario.MyDataClass>
    {
        public class DoSomething : StepBody
        {
            public int Counter { get; set; }

            public override ExecutionResult Run(IStepExecutionContext context)
            {
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public int Counter { get; set; }
        }

        public class ForeachSyncWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForeachSyncWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(_ => ExecutionResult.Next())
                    .ForEach(x => new List<int> { 10, 2, 3 }, _ => false)
                        .Do(x => x
                            .StartWith<Delay>()
                                .Input(step => step.Period, (_, context) => TimeSpan.FromSeconds((int)context.Item))
                            .Then<DoSomething>()
                                .Input(step => step.Counter, (data, context) => (int)context.Item)
                                .Output(data => data.Counter, step => step.Counter)
                        );
            }
        }

        public ForeachSyncScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            GetData(workflowId).Counter.Should().Be(3);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
