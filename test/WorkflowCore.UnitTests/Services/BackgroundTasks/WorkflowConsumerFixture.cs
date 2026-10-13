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
using WorkflowCore.Services;
using WorkflowCore.Services.BackgroundTasks;
using Xunit;

namespace WorkflowCore.UnitTests.Services.BackgroundTasks
{
    public class WorkflowConsumerFixture
    {
        private const string BrokenId = "broken";
        private const string HealthyId = "healthy";

        private readonly IPersistenceProvider _persistence = A.Fake<IPersistenceProvider>();
        private readonly IWorkflowExecutor _executor = A.Fake<IWorkflowExecutor>();
        private readonly IGreyList _greylist = A.Fake<IGreyList>();
        private readonly WorkflowOptions _options = new WorkflowOptions(A.Fake<IServiceCollection>());
        private readonly TestableWorkflowConsumer _subject;

        public WorkflowConsumerFixture()
        {
            A.CallTo(() => _persistence.GetWorkflowInstance(BrokenId, A<CancellationToken>._))
                .Throws(new InvalidOperationException("Unexpected end when reading JSON."));
            A.CallTo(() => _persistence.GetWorkflowInstance(HealthyId, A<CancellationToken>._))
                .ReturnsLazily(() => new WorkflowInstance { Id = HealthyId, Status = WorkflowStatus.Complete });

            _subject = new TestableWorkflowConsumer(_persistence, A.Fake<IQueueProvider>(), new LoggerFactory(), A.Fake<IServiceProvider>(),
                A.Fake<IWorkflowRegistry>(), new SingleNodeLockProvider(), _executor, A.Fake<IDateTimeProvider>(), _greylist, _options);
        }

        [Fact(DisplayName = "Should stop loading an instance after the configured number of consecutive load failures")]
        public async Task should_quarantine_unloadable_instance()
        {
            for (var i = 0; i < 20; i++)
                await _subject.Process(BrokenId).ContinueWith(t => { });

            A.CallTo(() => _persistence.GetWorkflowInstance(BrokenId, A<CancellationToken>._)).MustHaveHappened(5, Times.Exactly);
        }

        [Fact(DisplayName = "Should honour a custom max load attempts")]
        public async Task should_honour_custom_max_load_attempts()
        {
            _options.UseMaxWorkflowLoadAttempts(2);

            for (var i = 0; i < 20; i++)
                await _subject.Process(BrokenId).ContinueWith(t => { });

            A.CallTo(() => _persistence.GetWorkflowInstance(BrokenId, A<CancellationToken>._)).MustHaveHappened(2, Times.Exactly);
        }

        [Fact(DisplayName = "Should keep throwing load failures before quarantine so normal retry logging still happens")]
        public async Task should_rethrow_load_failure()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => _subject.Process(BrokenId));
        }

        [Fact(DisplayName = "Should leave a quarantined instance greylisted so the poller stops re-queuing it")]
        public async Task should_keep_quarantined_instance_greylisted()
        {
            for (var i = 0; i < 5; i++)
                await _subject.Process(BrokenId).ContinueWith(t => { });

            A.CallTo(() => _greylist.Add("wf:" + BrokenId)).MustHaveHappened();
            A.CallTo(() => _greylist.Remove("wf:" + BrokenId)).MustHaveHappened(4, Times.Exactly);
        }

        [Fact(DisplayName = "Should reset the failure count after a successful load")]
        public async Task should_reset_after_successful_load()
        {
            var calls = 0;
            A.CallTo(() => _persistence.GetWorkflowInstance("flaky", A<CancellationToken>._))
                .ReturnsLazily(() =>
                {
                    calls++;
                    if (calls % 4 == 0)
                        return Task.FromResult(new WorkflowInstance { Id = "flaky", Status = WorkflowStatus.Complete });
                    throw new TimeoutException();
                });

            for (var i = 0; i < 12; i++)
                await _subject.Process("flaky").ContinueWith(t => { });

            calls.Should().Be(12);
        }

        [Fact(DisplayName = "Should keep processing healthy instances while another is quarantined")]
        public async Task should_process_healthy_instances()
        {
            for (var i = 0; i < 10; i++)
                await _subject.Process(BrokenId).ContinueWith(t => { });

            await _subject.Process(HealthyId);
            await _subject.Process(HealthyId);

            A.CallTo(() => _persistence.GetWorkflowInstance(HealthyId, A<CancellationToken>._)).MustHaveHappened(2, Times.Exactly);
        }

        private class TestableWorkflowConsumer : WorkflowConsumer
        {
            public TestableWorkflowConsumer(IPersistenceProvider persistenceProvider, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, IDateTimeProvider datetimeProvider, IGreyList greylist, WorkflowOptions options)
                : base(persistenceProvider, queueProvider, loggerFactory, serviceProvider, registry, lockProvider, executor, datetimeProvider, greylist, options)
            {
            }

            public Task Process(string itemId) => ProcessItem(itemId, CancellationToken.None);
        }
    }
}
