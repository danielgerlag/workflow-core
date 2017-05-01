using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class BasicScenario : BaseScenario<BasicScenario.BasicWorkflow, Object>
    {
        static int Step1Ticker = 0;
        static int Step2Ticker = 0;

        public class Step1 : StepBody
        {
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step1Ticker++;
                return ExecutionResult.Next();
            }
        }

        public class BasicWorkflow : IWorkflow
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

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("BasicWorkflow").Result;
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                System.Threading.Thread.Sleep(100);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(1);
        }
    }
}
