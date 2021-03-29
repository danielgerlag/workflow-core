using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class UserScenario : WorkflowTest<UserScenario.HumanWorkflow, Object>
    {
        internal static int ApproveStepTicker = 0;
        internal static int DisapproveStepTicker = 0;

        public class HumanWorkflow : IWorkflow
        {
            public string Id => "HumanWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .UserTask("Do you approve", data => @"user1")
                        .WithOption("yes", "I approve").Do(then => then
                            .StartWith(context => ApproveStepTicker++)
                        )
                        .WithOption("no", "I do not approve").Do(then => then
                            .StartWith(context => DisapproveStepTicker++)
                    );
            }
        }

        public UserScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = StartWorkflow(null);
            var counter = 0;

            while ((!Host.GetOpenUserActions(workflowId).Any()) && (counter < 180))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
            }

            var openItems1 = Host.GetOpenUserActions(workflowId).ToList();

            Host.PublishUserAction(openItems1.First().Key, "user1", "yes").Wait();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            var openItems2 = Host.GetOpenUserActions(workflowId);        

            ApproveStepTicker.Should().Be(1);
            DisapproveStepTicker.Should().Be(0);
            openItems1.Count().Should().Be(1);
            openItems1.First().Options.Count().Should().Be(2);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "yes").Should().Be(true);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "no").Should().Be(true);
            openItems2.Count().Should().Be(0);
            UnhandledStepErrors.Count.Should().Be(0);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        }
    }
}
