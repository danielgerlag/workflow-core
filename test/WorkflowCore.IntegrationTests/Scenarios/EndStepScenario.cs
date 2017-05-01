using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class EndStepScenario : BaseScenario<EndStepScenario.ScenarioWorkflow, Object>
    {        
        internal static int StartStepCounter = 0;
        internal static int Branch1Counter = 0;
        internal static int Branch2Counter = 0;

        public class ScenarioWorkflow : IWorkflow
        {
            public string Id => "EndStepScenario";
            public int Version => 1;
            public void Build(IWorkflowBuilder<Object> builder)
            {
                var step1 = builder.StartWith(context =>
                {
                    StartStepCounter++;
                    return ExecutionResult.Outcome(1);
                });

                step1
                    .When(1)
                        .Then(context => ExecutionResult.Next());

                step1
                    .When(1)
                        .Then(context =>
                        {
                            Branch1Counter++;
                            return ExecutionResult.Next();
                        });
            }
        }

        //[Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("EndStepScenario").Result;
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            int counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 300))
            {
                System.Threading.Thread.Sleep(100);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }


        }

    }
}
