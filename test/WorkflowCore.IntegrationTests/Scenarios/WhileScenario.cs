using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class WhileScenario : BaseScenario<WhileScenario.WhileWorkflow, WhileScenario.MyDataClass>
    {
        static int Step1Ticker = 0;
        static int Step2Ticker = 0;
        static int Step3Ticker = 0;
        static int AfterLoopValue = 0;

        static DateTime LastWhileBlock;
        static DateTime AfterWhileBlock;

        public class DoSomething : StepBody
        {
            public int Counter { get; set; }
            public override ExecutionResult Run(IStepExecutionContext context)
            {
                Step2Ticker++;
                Counter = Step2Ticker;
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
                    .StartWith(context =>
                    {
                        Step1Ticker++;
                        return ExecutionResult.Next();
                    })
                    .While(x => x.Counter < 3).Do(x => x
                        .StartWith<DoSomething>()
                            .Output(data => data.Counter, step => step.Counter)
                        .Then(context =>
                        {
                            LastWhileBlock = DateTime.Now;
                            return ExecutionResult.Next();
                        })
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

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("WhileWorkflow", new MyDataClass() { Counter = 0 }).Result;
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
            Step2Ticker.Should().Be(3);
            Step3Ticker.Should().Be(1);
            AfterLoopValue.Should().Be(3);
            AfterWhileBlock.Should().BeAfter(LastWhileBlock);
        }
    }
}
