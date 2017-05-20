using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Threading;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class IfScenario : BaseScenario<IfScenario.IfWorkflow, IfScenario.MyDataClass>
    {
        static int Step1Ticker = 0;
        static int Step2Ticker = 0;
        static int If1Ticker = 0;
        static int If2Ticker = 0;
        static int If3Ticker = 0;
        static DateTime LastIfBlock;
        static DateTime AfterIfBlock;

        public class MyDataClass
        {
            public int Counter { get; set; }
        }

        public class IfWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "IfWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        Step1Ticker++;
                        return ExecutionResult.Next();
                    })
                    .If(data => data.Counter < 3).Do(then => then
                        .StartWith(context =>
                        {
                            If1Ticker++;
                            LastIfBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .If(data => data.Counter == 5).Do(then => then
                        .StartWith(context =>
                        {
                            If2Ticker++;
                            LastIfBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .If(data => data.Counter == 2).Do(then => then
                        .StartWith(context =>
                        {
                            If3Ticker++;
                            LastIfBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .Then(context =>
                    {
                        Step2Ticker++;
                        AfterIfBlock = DateTime.Now;
                        return ExecutionResult.Next();
                    });
            }
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("IfWorkflow", new MyDataClass() { Counter = 2 }).Result;
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                Thread.Sleep(100);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            instance.Status.Should().Be(WorkflowStatus.Complete);
            Step1Ticker.Should().Be(1);
            Step2Ticker.Should().Be(1);

            If1Ticker.Should().Be(1);
            If2Ticker.Should().Be(0);
            If3Ticker.Should().Be(1);

            AfterIfBlock.Should().BeAfter(LastIfBlock);
        }
    }
}
