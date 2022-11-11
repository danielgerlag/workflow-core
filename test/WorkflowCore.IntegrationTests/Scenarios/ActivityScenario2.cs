using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    /*
     * DISABLED for bug on build pipeline
    public class ActivityScenario2 : WorkflowTest<ActivityScenario2.ActivityWorkflow, ActivityScenario2.MyDataClass>
    {
        public class MyDataClass
        {
            public object ActivityInput { get; set; }
            public object ActivityOutput { get; set; }
        }

        public class ActivityInput
        {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
        }

        public class ActivityOutput
        {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
        }

        public class ActivityWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "ActivityWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Activity((_, context) => "act-1-" + context.Workflow.Id, data => data.ActivityInput)
                        .Output(data => data.ActivityOutput, step => step.Result);
            }
        }

        public ActivityScenario2()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            // compound key
            var workflowId = StartWorkflow(new MyDataClass { ActivityInput = new ActivityInput { Value1 = "a", Value2 = 1 } });
            var activity = Host.GetPendingActivity("act-1-" + workflowId, "worker1", TimeSpan.FromSeconds(30)).Result;

            if (activity != null)
            {
                var actInput = (ActivityInput)activity.Parameters;
                Host.SubmitActivitySuccess(activity.Token, new ActivityOutput
                { 
                    Value1 = actInput.Value1 + "1",
                    Value2 = actInput.Value2 + 1
                });
            }

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).ActivityOutput.Should().BeOfType<ActivityOutput>();
            var outData = (GetData(workflowId).ActivityOutput as ActivityOutput);
            outData.Value1.Should().Be("a1");
            outData.Value2.Should().Be(2);
        }
    }*/

}
