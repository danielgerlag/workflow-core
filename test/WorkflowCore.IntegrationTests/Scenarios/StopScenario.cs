using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;
using WorkflowCore.Models.LifeCycleEvents;
using System.Threading.Tasks;
using System.Threading;
using Moq;

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

        public StopScenario()
        {
            Setup();
        }

        [Fact]
        public async Task Scenario()
        {
            var tcs = new TaskCompletionSource<object>();
            Host.OnLifeCycleEvent += (evt) => OnLifeCycleEvent(evt, tcs);
            var workflowId = StartWorkflow(null);

            await tcs.Task;
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        }

        private async void OnLifeCycleEvent(LifeCycleEvent evt, TaskCompletionSource<object> tcs)
        {
            if (evt is WorkflowCompleted)
            {
                await Host.StopAsync(CancellationToken.None);
                tcs.SetResult(new());
            }
        }

        protected override void Dispose(bool disposing) { }
    }
}
