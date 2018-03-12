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
using System.Threading.Tasks;

using WorkflowCore.Testing;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class UserScenario : WorkflowTest<UserScenario.HumanWorkflow, UserScenario.ApprovalData>
    {
        public class ApprovalData
        {
            public int ApproveStepTicker { get; set; }
            public int DisapproveStepTicker { get; set; }
        }

        public class HumanWorkflow : IWorkflow<ApprovalData>
        {
            public string Id => "HumanWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<ApprovalData> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .UserTask("Do you approve", data => @"user1")
                        .WithOption("yes", "I approve").Do(then => then
                            .StartWith<ApprovalStep>()
                        )
                        .WithOption("no", "I do not approve").Do(then => then
                            .StartWith<DisapprovalStep>()
                    );
            }
        }

        public class ApprovalStep : IStepBody
        {
            public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
            {
                var data = (ApprovalData) context.Workflow.Data;
                data.ApproveStepTicker++;
                return Task.FromResult(ExecutionResult.Next());
            }
        }

        public class DisapprovalStep : IStepBody
        {
            public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
            {
                var data = (ApprovalData) context.Workflow.Data;
                data.DisapproveStepTicker++;
                return Task.FromResult(ExecutionResult.Next());
            }
        }

        public UserScenario()
        {
            Setup();
        }

        [Fact]
        public void ScenarioFluent()
        {
            var data = new ApprovalData();
            var workflowId = StartWorkflow(data);
            Scenario(workflowId, data).Wait();
        }

        [Fact]
        public async Task ScenarioJson()
        {
            var data = new ApprovalData();
            var workflowId = await Host.StartWorkflow<ApprovalData>("HumanWorkflowJson", data);
            await Scenario(workflowId, data);
        }

        private async Task Scenario(string workflowId, ApprovalData data)
        {
            var counter = 0;

            var instance = await PersistenceProvider.GetWorkflowInstance(workflowId);
            while ((!instance.GetOpenUserActions().Any()) && (counter < 5))
            {
                System.Threading.Thread.Sleep(200);
                counter++;
                instance = await PersistenceProvider.GetWorkflowInstance(workflowId);
            }

            var openItems1 = Host.GetOpenUserActions(workflowId).ToList();

            Host.PublishUserAction(openItems1.First().Key, "user1", "yes").Wait();

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            var openItems2 = Host.GetOpenUserActions(workflowId);        

            data.ApproveStepTicker.Should().Be(1);
            data.DisapproveStepTicker.Should().Be(0);
            openItems1.Count().Should().Be(1);
            openItems1.First().Options.Count().Should().Be(2);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "yes").Should().Be(true);
            openItems1.First().Options.Any(x => Convert.ToString(x.Value) == "no").Should().Be(true);
            openItems2.Count().Should().Be(0);
            UnhandledStepErrors.Count.Should().Be(0);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        }

        protected override void Setup()
        {
            base.Setup();

            var workflowAsJson = @"{
    ""Id"": ""HumanWorkflowJson"",
    ""Version"": 1,
    ""DataType"": ""WorkflowCore.IntegrationTests.Scenarios.UserScenario+ApprovalData, WorkflowCore.IntegrationTests"",
    ""Steps"": [
        {
            ""Id"": ""UserTask"",
            ""StepType"": ""WorkflowCore.Users.Primitives.UserTaskStep, WorkflowCore.Users"",
            ""Properties"": {
                ""Options"": {
                    ""I approve"": ""yes"",
                    ""I do not approve"": ""no""
                }
            },
            ""Inputs"": {
                ""AssignedPrincipal"": ""\""user1\"""",
                ""Prompt"": ""\""Do you approve\""""
            },
            ""Do"": [[
                {
                    ""Id"": ""Choice: yes"",
                    ""StepType"": ""WorkflowCore.Primitives.When, WorkflowCore"",
                    ""Inputs"": {
                        ""ExpectedOutcome"": ""\""yes\""""
                    },
                    ""Do"": [[
                        {
                            ""Id"": ""Approve"",
                            ""StepType"": ""WorkflowCore.IntegrationTests.Scenarios.UserScenario+ApprovalStep, WorkflowCore.IntegrationTests"",
                        }
                    ]]
                },
                {
                    ""Id"": ""Choice: No"",
                    ""StepType"": ""WorkflowCore.Primitives.When, WorkflowCore"",
                    ""Inputs"": {
                        ""ExpectedOutcome"": ""\""no\""""
                    },
                    ""Do"": [[
                        {
                            ""Id"": ""Disapprove"",
                            ""StepType"": ""WorkflowCore.IntegrationTests.Scenarios.UserScenario+DisapprovalStep, WorkflowCore.IntegrationTests"",
                        }
                    ]]
                }
            ]]
        }
    ]
}";

            DefinitionLoader.LoadDefinition(workflowAsJson);
        }
    }
}
