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
    public class ForeachScenario : WorkflowTest<ForeachScenario.ForeachWorkflow, ForeachScenario.MyDataClass>
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;
        internal static int Step3Ticker = 0;
        internal static int AfterLoopValue = 0;
        internal static int CheckSum = 0;

        public class DoSomething : StepBody
        {
            
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step2Ticker++;
                CheckSum += Convert.ToInt32(context.Item);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
        }

        public class ForeachWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ForeachWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        Step1Ticker++;
                        return ExecutionResult.Next();
                    })
                    .ForEach(x => new List<int>() { 2, 2, 3 })
                        .Do(x => x.StartWith<DoSomething>())                    
                    .Then(context =>
                    {
                        AfterLoopValue = Step2Ticker;
                        Step3Ticker++;
                        return ExecutionResult.Next();
                    });
            }
        }

        public ForeachScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(null);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(3);
            Step3Ticker.Should().Be(1);
            AfterLoopValue.Should().Be(3);
            CheckSum.Should().Be(7);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
