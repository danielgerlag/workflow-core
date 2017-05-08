using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using FluentAssertions.Collections;
using WorkflowCore.Users.Models;
using System.Linq;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class UserScenario : BaseScenario<UserScenario.HumanWorkflow, Object>
    {
        static int ApproveStepTicker = 0;
        static int DisapproveStepTicker = 0;

        public class HumanWorkflow : IWorkflow
        {
            public string Id => "HumanWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                .StartWith(context => ExecutionResult.Next())
                .UserStep("Do you approve", data => "user1", x => x.Name("Approval Step"))
                    .When("yes", "I approve")
                        .Then(context =>
                        {
                            ApproveStepTicker++;
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step")
                    .When("no", "I do not approve")
                        .Then(context =>
                        {
                            DisapproveStepTicker++;
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step");
            }
        }

        [Fact]
        public void Scenario()
        {
            var workflowId = Host.StartWorkflow("HumanWorkflow").Result;
            int counter = 0;

            while ((Host.GetOpenUserActions(workflowId).Count() == 0) && (counter < 180))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
            }

            var openItems1 = Host.GetOpenUserActions(workflowId);

            Host.PublishUserAction(openItems1.First().Key, "user1", "yes").Wait();

            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            counter = 0;
            while ((instance.Status == WorkflowStatus.Runnable) && (counter < 180))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
                instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            }

            var openItems2 = Host.GetOpenUserActions(workflowId);        

            ApproveStepTicker.Should().Be(1);
            DisapproveStepTicker.Should().Be(0);
            openItems1.Count().Should().Be(1);
            openItems1.First().Options.Count().Should().Be(2);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "yes").Should().Be(true);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "no").Should().Be(true);
        }
    }
}
