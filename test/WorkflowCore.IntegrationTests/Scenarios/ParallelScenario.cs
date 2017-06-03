using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class ParallelScenario : BaseScenario<ParallelScenario.ParallelWorkflow, ParallelScenario.MyDataClass>
    {
        private static int StartStepTicker = 0;
        private static int EndStepTicker = 0;

        private static int Step11Ticker = 0;
        private static int Step12Ticker = 0;
        private static int Step21Ticker = 0;
        private static int Step22Ticker = 0;
        private static int Step31Ticker = 0;
        private static int Step32Ticker = 0;

        public class MyDataClass
        {
        }

        public class ParallelWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ParallelWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(x => 
                    {
                        StartStepTicker++;
                        return ExecutionResult.Next();
                    })
                    .Parallel()
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step11Ticker++;
                            return ExecutionResult.Next();
                        })
                        .Then(x =>
                        {
                            Step12Ticker++;
                            return ExecutionResult.Next();
                        }))
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step21Ticker++;
                            return ExecutionResult.Next();
                        })
                        .WaitFor("MyEvent", data => "0")
                        .Then(x =>
                        {
                            Step22Ticker++;
                            return ExecutionResult.Next();
                        }))
                    .Do(then =>
                        then.StartWith(x =>
                        {
                            Step31Ticker++;
                            return ExecutionResult.Next();
                        })
                        .Then(x =>
                        {
                            Step32Ticker++;
                            return ExecutionResult.Next();
                        }))
                .Join()
                .Then(x =>
                {
                    EndStepTicker++;
                    return ExecutionResult.Next();
                });
            }
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("ParallelWorkflow", new MyDataClass()).Result;

            int counter = 0;
            while ((Step12Ticker == 0) && (Step32Ticker == 0) && (PersistenceProvider.GetSubcriptions("MyEvent", "0", DateTime.MaxValue).Result.Count() == 0) && (counter < 150))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
            }

            Step22Ticker.Should().Be(0);

            Host.PublishEvent("MyEvent", "0", "Pass");

            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 150))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            StartStepTicker.Should().Be(1);
            EndStepTicker.Should().Be(1);
            Step11Ticker.Should().Be(1);
            Step12Ticker.Should().Be(1);
            Step21Ticker.Should().Be(1);
            Step22Ticker.Should().Be(1);
            Step31Ticker.Should().Be(1);
            Step32Ticker.Should().Be(1);
        }
    }
}
