using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.BackgroundTasks;
using Xunit;

namespace WorkflowCore.UnitTests.Services.BackgroundTasks
{
    public class WorkflowConsumerTests
    {
        [Fact(DisplayName = "ProcessItem should not throw when the workflow instance is missing")]
        public async Task ProcessItem_WhenWorkflowInstanceIsNull_DoesNotThrow()
        {
            var persistence = A.Fake<IPersistenceProvider>();
            var queue = A.Fake<IQueueProvider>();
            var lockProvider = A.Fake<IDistributedLockProvider>();
            var executor = A.Fake<IWorkflowExecutor>();
            var datetime = A.Fake<IDateTimeProvider>();
            var greylist = A.Fake<IGreyList>();
            var options = new WorkflowOptions(A.Fake<IServiceCollection>());

            A.CallTo(() => lockProvider.AcquireLock("missing-id", A<CancellationToken>._))
                .Returns(true);
            A.CallTo(() => persistence.GetWorkflowInstance("missing-id", A<CancellationToken>._))
                .Returns(Task.FromResult<WorkflowInstance>(null));

            var consumer = new TestableWorkflowConsumer(
                persistence,
                queue,
                new LoggerFactory(),
                A.Fake<IServiceProvider>(),
                A.Fake<IWorkflowRegistry>(),
                lockProvider,
                executor,
                datetime,
                greylist,
                options);

            await consumer.Process("missing-id", CancellationToken.None);

            A.CallTo(() => executor.Execute(A<WorkflowInstance>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => greylist.Remove("wf:missing-id")).MustHaveHappenedOnceExactly();
            A.CallTo(() => lockProvider.ReleaseLock("missing-id")).MustHaveHappenedOnceExactly();
        }

        private class TestableWorkflowConsumer : WorkflowConsumer
        {
            public TestableWorkflowConsumer(
                IPersistenceProvider persistenceProvider,
                IQueueProvider queueProvider,
                ILoggerFactory loggerFactory,
                IServiceProvider serviceProvider,
                IWorkflowRegistry registry,
                IDistributedLockProvider lockProvider,
                IWorkflowExecutor executor,
                IDateTimeProvider datetimeProvider,
                IGreyList greylist,
                WorkflowOptions options)
                : base(persistenceProvider, queueProvider, loggerFactory, serviceProvider, registry, lockProvider, executor, datetimeProvider, greylist, options)
            {
            }

            public Task Process(string itemId, CancellationToken cancellationToken) => ProcessItem(itemId, cancellationToken);
        }
    }
}
