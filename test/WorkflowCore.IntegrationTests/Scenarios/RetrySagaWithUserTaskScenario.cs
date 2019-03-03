using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Testing;
using WorkflowCore.Users.Models;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class RetrySagaWithUserTaskScenario : WorkflowTest<RetrySagaWithUserTaskScenario.Workflow, RetrySagaWithUserTaskScenario.MyDataClass>
    {
        public class MyDataClass
        {
        }

        public class Workflow : IWorkflow<MyDataClass>
        {
            public static int Event1Fired;
            public static int Event2Fired;
            public static int Event3Fired;
            public static int TailEventFired;
            public static int Compensation1Fired;
            public static int Compensation2Fired;
            public static int Compensation3Fired;
            public static int Compensation4Fired;

            public string Id => "RetrySagaWithUserTaskWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .CompensateWith(context => Compensation1Fired++)
                    .Saga(x => x
                        .StartWith(context => ExecutionResult.Next())
                            .CompensateWith(context => Compensation2Fired++)
                        .UserTask("prompt", data => "assigner")
                            .WithOption("a", "Option A")
                                .Do(wb => wb
                                    .StartWith(context => ExecutionResult.Next())
                                    .Then(context =>
                                    {
                                        Event1Fired++;
                                        if (Event1Fired < 3)
                                            throw new Exception();
                                        Event2Fired++;
                                    })
                                        .CompensateWith(context => Compensation3Fired++)
                                    .Then(context => Event3Fired++)
                                        .CompensateWith(context => Compensation4Fired++)
                                )
                    )
                    .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(1))
                    .Then(context => TailEventFired++);
            }
        }

        public RetrySagaWithUserTaskScenario()
        {
            Setup();
            Workflow.Event1Fired = 0;
            Workflow.Event2Fired = 0;
            Workflow.Event3Fired = 0;
            Workflow.Compensation1Fired = 0;
            Workflow.Compensation2Fired = 0;
            Workflow.Compensation3Fired = 0;
            Workflow.Compensation4Fired = 0;
            Workflow.TailEventFired = 0;
        }

        [Fact]
        public async Task Scenario()
        {
            var workflowId = StartWorkflow(new MyDataClass());
            var instance = await Host.PersistenceStore.GetWorkflowInstance(workflowId);

            string oldUserOptionKey = null;
            for (var i = 0; i != 3; ++i)
            {
                var userOptions = await WaitForDifferentUserStepAsync(instance, TimeSpan.FromSeconds(1), oldUserOptionKey);
                userOptions.Count.Should().Be(1);

                var userOption = userOptions.Single();
                userOption.Prompt.Should().Be("prompt");
                userOption.AssignedPrincipal.Should().Be("assigner");
                userOption.Options.Count.Should().Be(1);

                var selectionOption = userOption.Options.Single();
                selectionOption.Key.Should().Be("Option A");
                selectionOption.Value.Should().Be("a");
                await Host.PublishUserAction(userOption.Key, string.Empty, selectionOption.Value);

                oldUserOptionKey = userOption.Key;
            }

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(2);
            Workflow.Event1Fired.Should().Be(3);
            Workflow.Event2Fired.Should().Be(1);
            Workflow.Event3Fired.Should().Be(1);
            Workflow.Compensation1Fired.Should().Be(0);
            Workflow.Compensation2Fired.Should().Be(2);
            Workflow.Compensation3Fired.Should().Be(2);
            Workflow.Compensation4Fired.Should().Be(0);
            Workflow.TailEventFired.Should().Be(1);
        }

        private static async Task<IReadOnlyCollection<OpenUserAction>> WaitForDifferentUserStepAsync(
            WorkflowInstance instance,
            TimeSpan timeout,
            string oldUserActionKey = null)
        {
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime <= timeout)
            {
                var userActions = await WaitForUserStepAsync(instance);

                if (oldUserActionKey != null && userActions.Any(x => x.Key == oldUserActionKey))
                {
                    continue;
                }

                return userActions;
            }

            return Array.Empty<OpenUserAction>();
        }

        private static async Task<IReadOnlyCollection<OpenUserAction>> WaitForUserStepAsync(WorkflowInstance instance)
        {
            var delayCount = 200;
            var openActions = instance.GetOpenUserActions()?.ToList();
            while ((openActions?.Count ?? 0) == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                openActions = instance.GetOpenUserActions()?.ToList();
                if (delayCount-- == 0)
                {
                    break;
                }
            }

            return openActions;
        }
    }
}