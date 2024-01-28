using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    [Obsolete]
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
        public async Task Scenario()
        {
            var workflowId = await Host.StartWorkflow("OutcomeFork");
            var instance = await PersistenceProvider.GetWorkflowInstance(workflowId);
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                System.Threading.Thread.Sleep(100);
                counter++;
                instance = await PersistenceProvider.GetWorkflowInstance(workflowId);
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            TaskATicker.Should().Be(1);
            TaskBTicker.Should().Be(0);
            TaskCTicker.Should().Be(1);
        }
    }
}
