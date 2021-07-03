using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;
using System.Threading;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public sealed class ParallelEventsScenario
        : WorkflowTest<ParallelEventsScenario.ParallelEventsWorkflow, ParallelEventsScenario.MyDataClass>
    {
        private const string EVENT_KEY = nameof(EVENT_KEY);

        public class MyDataClass
        {
            public string StrValue1 { get; set; }
            public string StrValue2 { get; set; }
        }

        public class ParallelEventsWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Parallel()
                    .Do(then =>
                        then.WaitFor("Event1", data => EVENT_KEY).Then(x =>
                        {
                            Thread.Sleep(300);
                            return ExecutionResult.Next();
                        }))
                    .Do(then =>
                        then.WaitFor("Event2", data => EVENT_KEY).Then(x =>
                        {
                            Thread.Sleep(100);
                            return ExecutionResult.Next();
                        }))
                   .Do(then =>
                        then.WaitFor("Event3", data => EVENT_KEY).Then(x =>
                        {
                            Thread.Sleep(1000);
                            return ExecutionResult.Next();
                        }))
                   .Do(then =>
                        then.WaitFor("Event4", data => EVENT_KEY).Then(x =>
                        {
                            Thread.Sleep(100);
                            return ExecutionResult.Next();
                        }))
                   .Do(then =>
                        then.WaitFor("Event5", data => EVENT_KEY).Then(x =>
                        {
                            Thread.Sleep(100);
                            return ExecutionResult.Next();
                        }))
                .Join()
                .Then(x =>
                {
                    return ExecutionResult.Next();
                });
            }
        }

        public ParallelEventsScenario()
        {
            Setup();
        }

        [Fact]
        public void Scenario()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = StartWorkflow(new MyDataClass { StrValue1 = eventKey, StrValue2 = eventKey });
            Host.PublishEvent("Event1", EVENT_KEY, "Pass1");
            Host.PublishEvent("Event2", EVENT_KEY, "Pass2");
            Host.PublishEvent("Event3", EVENT_KEY, "Pass3");
            Host.PublishEvent("Event4", EVENT_KEY, "Pass4");
            Host.PublishEvent("Event5", EVENT_KEY, "Pass5");

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
