using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class StopAsyncWorkflow : IWorkflow
    {
        internal static DateTime? StepStartTime = null;
        internal static DateTime? StepEndTime = null;

        public string Id => "StopAsyncWorkflow";
        public int Version => 1;
        public void Build(IWorkflowBuilder<Object> builder)
        {
            builder
                .StartWith<LongRunningStep>();
        }
    }

    internal class LongRunningStep : StepBodyAsync
    {
        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            StopAsyncWorkflow.StepStartTime = DateTime.Now;
            await Task.Delay(5000); // 5 second delay
            StopAsyncWorkflow.StepEndTime = DateTime.Now;
            return ExecutionResult.Next();
        }
    }

    public class StopAsyncScenario : IDisposable
    {   
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;

        public StopAsyncScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow(options => options.UsePollInterval(TimeSpan.FromSeconds(3)));

            var serviceProvider = services.BuildServiceProvider();

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.RegisterWorkflow<StopAsyncWorkflow, Object>();
            Host.Start();
        }

        [Fact]
        public async Task StopAsync_should_wait_for_running_steps_to_complete()
        {
            // Arrange
            StopAsyncWorkflow.StepStartTime = null;
            StopAsyncWorkflow.StepEndTime = null;

            // Start a workflow with a long-running step
            var workflowId = await Host.StartWorkflow<Object>("StopAsyncWorkflow", null);
            
            // Wait for the step to start executing
            var waitCount = 0;
            while (StopAsyncWorkflow.StepStartTime == null && waitCount < 50)
            {
                await Task.Delay(100);
                waitCount++;
            }
            
            StopAsyncWorkflow.StepStartTime.Should().NotBeNull("the step should have started before stopping");
            StopAsyncWorkflow.StepEndTime.Should().BeNull("the step should still be running");

            // Act - Call StopAsync which should wait for the step to complete
            var stopCalledAt = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();
            await Host.StopAsync(default);
            stopwatch.Stop();
            var stopReturnedAt = DateTime.Now;

            // Assert
            // The step should have completed
            StopAsyncWorkflow.StepEndTime.Should().NotBeNull("the step should have completed before StopAsync returned");
            
            // StopAsync must not return before the running step finished.
            StopAsyncWorkflow.StepEndTime.Value.Should().BeOnOrBefore(stopReturnedAt,
                "StopAsync should wait for the running step to complete");

            // StopAsync should have blocked for the step's remaining time. This is measured from
            // when the step actually started rather than a fixed 3s, because on a loaded CI runner
            // the polling loop above can observe StepStartTime late.
            var remaining = StopAsyncWorkflow.StepStartTime.Value.AddSeconds(5) - stopCalledAt;
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(remaining - TimeSpan.FromMilliseconds(250),
                "StopAsync should wait for the running step's remaining time");
        }

        public void Dispose()
        {
            // Dispose is intentionally empty to avoid double-stop
        }
    }
}
