using System;
using System.Threading.Tasks;
using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using Xunit;

namespace WorkflowCore.UnitTests.Services
{
    public class MemoryPersistenceProviderFixture : BasePersistenceFixture
    {
        private readonly IPersistenceProvider _subject = new MemoryPersistenceProvider();

        protected override IPersistenceProvider Subject => _subject;

        private IWorkflowPurger Purger => (IWorkflowPurger)_subject;

        [Fact]
        public async Task PurgeWorkflows_should_remove_terminated_instances()
        {
            var terminatedWorkflow = new WorkflowInstance
            {
                Status = WorkflowStatus.Terminated,
                CreateTime = DateTime.UtcNow.AddDays(-2),
                CompleteTime = DateTime.UtcNow.AddDays(-2),
                WorkflowDefinitionId = "terminated"
            };

            var activeWorkflow = new WorkflowInstance
            {
                Status = WorkflowStatus.Runnable,
                CreateTime = DateTime.UtcNow.AddDays(-2),
                NextExecution = 0,
                WorkflowDefinitionId = "active"
            };

            var terminatedId = await _subject.CreateNewWorkflow(terminatedWorkflow);
            var activeId = await _subject.CreateNewWorkflow(activeWorkflow);

            await Purger.PurgeWorkflows(WorkflowStatus.Terminated, DateTime.UtcNow.AddDays(-1));

            var remaining = await _subject.GetWorkflowInstances(new[] { terminatedId, activeId });
            remaining.Should().ContainSingle(x => x.Id == activeId);
        }

        [Fact]
        public async Task PurgeWorkflows_should_remove_completed_instances()
        {
            var completedWorkflow = new WorkflowInstance
            {
                Status = WorkflowStatus.Complete,
                CreateTime = DateTime.UtcNow.AddDays(-3),
                CompleteTime = DateTime.UtcNow.AddDays(-3),
                WorkflowDefinitionId = "completed"
            };

            var activeWorkflow = new WorkflowInstance
            {
                Status = WorkflowStatus.Runnable,
                CreateTime = DateTime.UtcNow.AddDays(-1),
                NextExecution = 0,
                WorkflowDefinitionId = "active"
            };

            var completedId = await _subject.CreateNewWorkflow(completedWorkflow);
            var activeId = await _subject.CreateNewWorkflow(activeWorkflow);

            await Purger.PurgeWorkflows(WorkflowStatus.Complete, DateTime.UtcNow.AddDays(-2));

            var remaining = await _subject.GetWorkflowInstances(new[] { completedId, activeId });
            remaining.Should().ContainSingle(x => x.Id == activeId);
        }
    }
}
