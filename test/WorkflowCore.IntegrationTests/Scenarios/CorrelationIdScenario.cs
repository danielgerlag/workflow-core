using FluentAssertions;
using System;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class CorrelationIdScenario : WorkflowTest<CorrelationIdScenario.CorrelationIdWorkflow, object>
    {
        public class CorrelationIdWorkflow : IWorkflow
        {
            public string Id => "CorrelationIdWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next());
            }
        }

        public CorrelationIdScenario()
        {
            Setup();
        }

        [Fact]
        public async Task Scenario()
        {
            var correlationId = Guid.NewGuid().ToString();
            var def = new CorrelationIdWorkflow();
            var workflowId = await Host.StartWorkflow(def.Id, correlationId: correlationId);

            var workflowId2 = await Host.StartWorkflow(def.Id, correlationId: correlationId);

            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);

            workflowId2.Should().Be(workflowId);
        }
    }
}
