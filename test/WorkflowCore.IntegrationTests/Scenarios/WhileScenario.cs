using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Threading;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class WhileScenario : WorkflowTest<WhileScenario.WhileWorkflow, WhileScenario.MyDataClass>
    {
        internal static int Step1Ticker = 0;
        internal static int Step2Ticker = 0;
        internal static int Step3Ticker = 0;
        internal static int AfterLoopValue = 0;

        internal static DateTime LastWhileBlock;
        internal static DateTime AfterWhileBlock;

        public class DoSomething : StepBody
        {
            public int Counter { get; set; }
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step2Ticker++;
                Counter = Step2Ticker;
                Thread.Sleep(50);
                return ExecutionResult.Next();
            }
        }

        public class MyDataClass
        {
            public int Counter { get; set; }
        }

        public class WhileWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "WhileWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => Step1Ticker++)
                    .While(x => x.Counter < 3).Do(x => x
                        .StartWith<DoSomething>()
                            .Output(data => data.Counter, step => step.Counter)
                        .Then(context => Thread.Sleep(500))
                        .Then(context => LastWhileBlock = DateTime.Now)
                    )                    
                    .Then(context =>
                    {
                        AfterLoopValue = Step2Ticker;
                        Step3Ticker++;
                        AfterWhileBlock = DateTime.Now;
                        return ExecutionResult.Next();
                    });
            }
        }

        public WhileScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass { Counter = 0 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
                        
            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(3);
            Step3Ticker.Should().Be(1);
            AfterLoopValue.Should().Be(3);
            AfterWhileBlock.Should().BeAfter(LastWhileBlock);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
