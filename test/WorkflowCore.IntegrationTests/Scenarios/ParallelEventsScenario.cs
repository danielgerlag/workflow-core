using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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

        public class SomeTask : StepBodyAsync
        {
            public TimeSpan Delay { get; set; }

            public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
            {
                await Task.Delay(Delay);

                return ExecutionResult.Next();
            }
        }

        public class ParallelEventsWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => nameof(ParallelEventsScenario);
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .Parallel()
                    .Do(then =>
                        then.WaitFor("Event1", data => EVENT_KEY).Then<SomeTask>()
                            .Input(step => step.Delay, data => TimeSpan.FromMilliseconds(2000)))
                    .Do(then =>
                        then.WaitFor("Event2", data => EVENT_KEY).Then<SomeTask>()
                            .Input(step => step.Delay, data => TimeSpan.FromMilliseconds(2000)))
                   .Do(then =>
                        then.WaitFor("Event3", data => EVENT_KEY).Then<SomeTask>()
                            .Input(step => step.Delay, data => TimeSpan.FromMilliseconds(5000)))
                   .Do(then =>
                        then.WaitFor("Event4", data => EVENT_KEY).Then<SomeTask>()
                            .Input(step => step.Delay, data => TimeSpan.FromMilliseconds(100)))
                   .Do(then =>
                        then.WaitFor("Event5", data => EVENT_KEY).Then<SomeTask>()
                            .Input(step => step.Delay, data => TimeSpan.FromMilliseconds(100)))
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

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(s => s.UsePollInterval(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task Scenario()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = await StartWorkflowAsync(new MyDataClass { StrValue1 = eventKey, StrValue2 = eventKey });
            await Host.PublishEvent("Event1", EVENT_KEY, "Pass1");
            await Host.PublishEvent("Event2", EVENT_KEY, "Pass2");
            await Host.PublishEvent("Event3", EVENT_KEY, "Pass3");
            await Host.PublishEvent("Event4", EVENT_KEY, "Pass4");
            await Host.PublishEvent("Event5", EVENT_KEY, "Pass5");

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
        }
    }
}
