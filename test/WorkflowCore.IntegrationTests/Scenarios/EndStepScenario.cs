using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    //public class EndStepScenario : BaseScenario<EndStepScenario.ScenarioWorkflow, Object>
    //{        
    //    internal static int StartStepCounter = 0;
    //    internal static int MidStepCounter = 0;
    //    internal static int EndStepCounter = 0;

    //    public class ScenarioWorkflow : IWorkflow
    //    {
    //        public string Id => "EndStepScenario";
    //        public int Version => 1;
    //        public void Build(IWorkflowBuilder<Object> builder)
    //        {
    //            builder
    //                .StartWith(context =>
    //                {
    //                    StartStepCounter++;
    //                    return ExecutionResult.Next();
    //                })
    //                .While(x => true)
    //                .Do(x => x
    //                    .StartWith(context =>
    //                    {
    //                        MidStepCounter++;
    //                        return ExecutionResult.Next();
    //                    }))
    //                    .EndWorkflow()
    //                .Then(context =>
    //                {
    //                    EndStepCounter++;
    //                    return ExecutionResult.Next();
    //                });
    //        }
    //    }

    //    [Fact]
    //    public void Scenario()
    //    {
    //        var workflowId = Host.StartWorkflow("EndStepScenario").Result;
    //        var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
    //        int counter = 0;
    //        while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
    //        {
    //            System.Threading.Thread.Sleep(100);
    //            counter++;
    //            instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
    //        }

    //        instance.Status.Should().Be(WorkflowStatus.Complete);
    //        StartStepCounter.Should().Be(1);
    //        MidStepCounter.Should().Be(1);
    //        EndStepCounter.Should().Be(0);
    //    }
    //}
}
