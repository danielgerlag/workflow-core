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
    public class WhenScenario : BaseScenario<WhenScenario.WhenWorkflow, WhenScenario.MyDataClass>
    {        
        static int Case1Ticker = 0;
        static int Case2Ticker = 0;
        static int Case3Ticker = 0;
        static DateTime LastBlock;
        static DateTime AfterBlock;

        public class MyDataClass
        {
            public int Counter { get; set; }
        }

        public class WhenWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "WhenWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        return ExecutionResult.Outcome(2);
                    })
                    .When(data => 1).Do(then => then
                        .StartWith(context =>
                        {
                            Case1Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .When(data => 2).Do(then => then
                        .StartWith(context =>
                        {
                            Case2Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .When(data => 2).Do(then => then
                        .StartWith(context =>
                        {
                            Case3Ticker++;
                            LastBlock = DateTime.Now;
                            Thread.Sleep(200);
                            return ExecutionResult.Next();
                        }))
                    .Then(context =>
                    {
                        AfterBlock = DateTime.Now;
                        return ExecutionResult.Next();
                    });
            }
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("WhenWorkflow", new MyDataClass() { Counter = 2 }).Result;
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                Thread.Sleep(100);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            Case1Ticker.Should().Be(0);
            Case2Ticker.Should().Be(1);
            Case3Ticker.Should().Be(1);
            AfterBlock.Should().BeAfter(LastBlock);
            instance.Status.Should().Be(WorkflowStatus.Complete);
        }
    }
}