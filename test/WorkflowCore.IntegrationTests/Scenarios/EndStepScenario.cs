using FluentAssertions;
using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class EndStepScenario : WorkflowTest<EndStepScenario.ScenarioWorkflow, Object>
    {
        internal static int StartStepCounter = 0;
        internal static int MidStepCounter = 0;
        internal static int EndStepCounter = 0;

        private int workflowCompletedEventTriggered = 0;

        public class ScenarioWorkflow : IWorkflow
        {
            public string Id => "EndStepScenario";
            public int Version => 1;
            public void Build(IWorkflowBuilder<Object> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        StartStepCounter++;
                        return ExecutionResult.Next();
                    })
                    .While(x => true)
                    .Do(x => x
                        .StartWith(context =>
                        {
                            MidStepCounter++;
                            return ExecutionResult.Next();
                        })
                        .EndWorkflow())                        
                    .Then(context =>
                    {
                        EndStepCounter++;
                        return ExecutionResult.Next();
                    });
            }
        }

        public EndStepScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            Host.OnLifeCycleEvent += (e) =>
            {
                if (e is WorkflowCompleted)
                {
                    workflowCompletedEventTriggered++;
                }
            };
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            StartStepCounter.Should().Be(1);
            MidStepCounter.Should().Be(1);
            EndStepCounter.Should().Be(0);
            workflowCompletedEventTriggered.Should().Be(1);
        }
    }
}
