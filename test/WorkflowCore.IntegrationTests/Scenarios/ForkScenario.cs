using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ForkScenario : BaseScenario<ForkScenario.OutcomeFork, Object>
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

        public class OutcomeFork : IWorkflow
        {
            public string Id => "OutcomeFork";
            public int Version => 1;
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

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("OutcomeFork").Result;
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                System.Threading.Thread.Sleep(100);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            TaskATicker.Should().Be(1);
            TaskBTicker.Should().Be(0);
            TaskCTicker.Should().Be(1);
        }
    }
}
