using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class StopScenario : WorkflowTest<StopScenario.StopWorkflow, object>
    {
        public class StopWorkflow : IWorkflow
        {
            public string Id => "StopWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder.StartWith(context => ExecutionResult.Next());
            }
        }

        public StopScenario() => Setup();

        [Fact]
        public async Task Scenario()
        {
            var tcs = new TaskCompletionSource<object>();
            Host.OnLifeCycleEvent += async (evt) =>
            {
                if (evt is WorkflowCompleted)
                {
                    await Host.StopAsync(CancellationToken.None);
                    tcs.SetResult(default);
                }
            };

            var workflowId = StartWorkflow(default);
            await tcs.Task;

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        }

        protected override void Dispose(bool disposing) { }
    }
}
