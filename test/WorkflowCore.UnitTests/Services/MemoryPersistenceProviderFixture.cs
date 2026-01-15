using System;
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
        public void PurgeWorkflows_should_remove_terminated_instances()
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

            var terminatedId = _subject.CreateNewWorkflow(terminatedWorkflow).Result;
            var activeId = _subject.CreateNewWorkflow(activeWorkflow).Result;

            Purger.PurgeWorkflows(WorkflowStatus.Terminated, DateTime.UtcNow.AddDays(-1)).Wait();

            var remaining = _subject.GetWorkflowInstances(new[] { terminatedId, activeId }).Result;
            remaining.Should().ContainSingle(x => x.Id == activeId);
        }

        [Fact]
        public void PurgeWorkflows_should_remove_completed_instances()
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

            var completedId = _subject.CreateNewWorkflow(completedWorkflow).Result;
            var activeId = _subject.CreateNewWorkflow(activeWorkflow).Result;

            Purger.PurgeWorkflows(WorkflowStatus.Complete, DateTime.UtcNow.AddDays(-2)).Wait();

            var remaining = _subject.GetWorkflowInstances(new[] { completedId, activeId }).Result;
            remaining.Should().ContainSingle(x => x.Id == activeId);
        }
    }
}
