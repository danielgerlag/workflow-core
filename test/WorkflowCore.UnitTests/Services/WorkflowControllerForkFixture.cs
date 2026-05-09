using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services;
using Xunit;

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowControllerForkFixture
    {
        protected WorkflowController Subject;
        protected IPersistenceProvider PersistenceProvider;
        protected IDistributedLockProvider LockProvider;
        protected IWorkflowRegistry Registry;
        protected IQueueProvider QueueProvider;
        protected IExecutionPointerFactory PointerFactory;
        protected ILifeCycleEventHub EventHub;
        protected IServiceProvider ServiceProvider;
        protected IDateTimeProvider DateTimeProvider;
        protected IWorkflowInstanceCloner Cloner;

        public WorkflowControllerForkFixture()
        {
            PersistenceProvider = A.Fake<IPersistenceProvider>();
            LockProvider = A.Fake<IDistributedLockProvider>();
            Registry = A.Fake<IWorkflowRegistry>();
            QueueProvider = A.Fake<IQueueProvider>();
            PointerFactory = A.Fake<IExecutionPointerFactory>();
            EventHub = A.Fake<ILifeCycleEventHub>();
            ServiceProvider = A.Fake<IServiceProvider>();
            DateTimeProvider = A.Fake<IDateTimeProvider>();
            Cloner = A.Fake<IWorkflowInstanceCloner>();

            A.CallTo(() => DateTimeProvider.UtcNow).Returns(DateTime.UtcNow);

            var loggerFactory = new LoggerFactory();
            Subject = new WorkflowController(
                PersistenceProvider, LockProvider, Registry, QueueProvider,
                PointerFactory, EventHub, loggerFactory, ServiceProvider,
                DateTimeProvider, Cloner);
        }

        [Fact(DisplayName = "Should fork runnable workflow")]
        public async Task should_fork_runnable_workflow()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Runnable);
            var cloneWorkflow = BuildClone("fork-reference", "fork-definition", 7);

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).Returns((cloneWorkflow, new List<EventSubscription>()));
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).Returns("fork-id");
            A.CallTo(() => QueueProvider.QueueWork(A<string>.Ignored, A<QueueType>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => EventHub.PublishNotification(A<LifeCycleEvent>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            var result = await Subject.ForkWorkflow("source-id");

            // assert
            result.Should().Be("fork-id");
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).MustHaveHappened();
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => QueueProvider.QueueWork("fork-id", QueueType.Workflow)).MustHaveHappened();
            A.CallTo(() => QueueProvider.QueueWork("fork-id", QueueType.Index)).MustHaveHappened();
            A.CallTo(() => EventHub.PublishNotification(A<WorkflowForked>.That.Matches(e =>
                e.SourceWorkflowInstanceId == "source-id" &&
                e.WorkflowInstanceId == "fork-id" &&
                e.WorkflowDefinitionId == "fork-definition" &&
                e.Version == 7 &&
                e.Reference == "fork-reference"))).MustHaveHappened();
            A.CallTo(() => LockProvider.ReleaseLock("source-id")).MustHaveHappened();
        }

        [Fact(DisplayName = "Should throw when lock is not acquired")]
        public async Task should_throw_when_lock_not_acquired()
        {
            // arrange
            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(false);

            // act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Subject.ForkWorkflow("source-id"));

            // assert
            exception.Message.Should().Be("Could not acquire lock on workflow instance.");
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance(A<string>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should throw when workflow is complete")]
        public async Task should_throw_when_workflow_is_complete()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Complete);

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Subject.ForkWorkflow("source-id"));

            // assert
            exception.Message.Should().Be("Cannot fork a workflow instance with status Complete.");
            A.CallTo(() => Cloner.CloneForFork(A<WorkflowInstance>.Ignored, A<Action<object>>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => LockProvider.ReleaseLock("source-id")).MustHaveHappened();
        }

        [Fact(DisplayName = "Should throw when workflow is terminated")]
        public async Task should_throw_when_workflow_is_terminated()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Terminated);

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Subject.ForkWorkflow("source-id"));

            // assert
            exception.Message.Should().Be("Cannot fork a workflow instance with status Terminated.");
            A.CallTo(() => Cloner.CloneForFork(A<WorkflowInstance>.Ignored, A<Action<object>>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => LockProvider.ReleaseLock("source-id")).MustHaveHappened();
        }

        [Fact(DisplayName = "Should fork suspended workflow")]
        public async Task should_fork_suspended_workflow()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Suspended);
            var cloneWorkflow = BuildClone();

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).Returns((cloneWorkflow, new List<EventSubscription>()));
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).Returns("fork-id");
            A.CallTo(() => QueueProvider.QueueWork(A<string>.Ignored, A<QueueType>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => EventHub.PublishNotification(A<LifeCycleEvent>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            var result = await Subject.ForkWorkflow("source-id");

            // assert
            result.Should().Be("fork-id");
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).MustHaveHappened();
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).MustHaveHappened();
        }

        [Fact(DisplayName = "Should create event subscriptions")]
        public async Task should_create_event_subscriptions()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Runnable);
            var cloneWorkflow = BuildClone();
            var subscriptions = new List<EventSubscription>
            {
                new EventSubscription { EventName = "evt-1", EventKey = "key-1" },
                new EventSubscription { EventName = "evt-2", EventKey = "key-2" }
            };

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).Returns((cloneWorkflow, subscriptions));
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).Returns("fork-id");
            A.CallTo(() => PersistenceProvider.CreateEventSubscription(A<EventSubscription>.Ignored, A<CancellationToken>.Ignored)).Returns("subscription-id");
            A.CallTo(() => QueueProvider.QueueWork(A<string>.Ignored, A<QueueType>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => EventHub.PublishNotification(A<LifeCycleEvent>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            await Subject.ForkWorkflow("source-id");

            // assert
            subscriptions.Should().OnlyContain(x => x.WorkflowId == "fork-id");
            A.CallTo(() => PersistenceProvider.CreateEventSubscription(subscriptions[0], A<CancellationToken>.Ignored)).MustHaveHappened();
            A.CallTo(() => PersistenceProvider.CreateEventSubscription(subscriptions[1], A<CancellationToken>.Ignored)).MustHaveHappened();
        }

        [Fact(DisplayName = "Should release lock on exception")]
        public async Task should_release_lock_on_exception()
        {
            // arrange
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Runnable);

            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).Throws(new ApplicationException("Clone failed"));
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            var exception = await Assert.ThrowsAsync<ApplicationException>(() => Subject.ForkWorkflow("source-id"));

            // assert
            exception.Message.Should().Be("Clone failed");
            A.CallTo(() => LockProvider.ReleaseLock("source-id")).MustHaveHappened();
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(A<WorkflowInstance>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should publish lifecycle event with correct data")]
        public async Task should_publish_lifecycle_event_with_correct_data()
        {
            // arrange
            var now = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var sourceWorkflow = BuildWorkflow("source-id", WorkflowStatus.Runnable);
            var cloneWorkflow = BuildClone("fork-reference", "fork-definition", 7);

            A.CallTo(() => DateTimeProvider.UtcNow).Returns(now);
            A.CallTo(() => LockProvider.AcquireLock(A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(true);
            A.CallTo(() => PersistenceProvider.GetWorkflowInstance("source-id", A<CancellationToken>.Ignored)).Returns(sourceWorkflow);
            A.CallTo(() => Cloner.CloneForFork(sourceWorkflow, null)).Returns((cloneWorkflow, new List<EventSubscription>()));
            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(cloneWorkflow, A<CancellationToken>.Ignored)).Returns("fork-id");
            A.CallTo(() => QueueProvider.QueueWork(A<string>.Ignored, A<QueueType>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => EventHub.PublishNotification(A<LifeCycleEvent>.Ignored)).Returns(Task.CompletedTask);
            A.CallTo(() => LockProvider.ReleaseLock(A<string>.Ignored)).Returns(Task.CompletedTask);

            // act
            await Subject.ForkWorkflow("source-id");

            // assert
            A.CallTo(() => EventHub.PublishNotification(A<WorkflowForked>.That.Matches(e =>
                e.SourceWorkflowInstanceId == "source-id" &&
                e.WorkflowInstanceId == "fork-id" &&
                e.WorkflowDefinitionId == "fork-definition" &&
                e.Version == 7 &&
                e.Reference == "fork-reference" &&
                e.EventTimeUtc == now))).MustHaveHappened();
        }

        private static WorkflowInstance BuildWorkflow(string id, WorkflowStatus status)
        {
            return new WorkflowInstance
            {
                Id = id,
                WorkflowDefinitionId = "source-definition",
                Version = 3,
                Reference = "source-reference",
                Status = status
            };
        }

        private static WorkflowInstance BuildClone(string reference = "fork-reference", string workflowDefinitionId = "fork-definition", int version = 7)
        {
            return new WorkflowInstance
            {
                WorkflowDefinitionId = workflowDefinitionId,
                Version = version,
                Reference = reference,
                Status = WorkflowStatus.Runnable
            };
        }
    }
}
