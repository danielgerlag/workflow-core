using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Services.BackgroundTasks;
using Xunit;

namespace WorkflowCore.UnitTests.Services.BackgroundTasks
{
    // Regression tests for #1462: unmatched events were re-processed on every poll.
    public class UnmatchedEventFixture
    {
        private const string EventId = "evt1";
        private static readonly DateTime Now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private readonly IPersistenceProvider _persistence = A.Fake<IPersistenceProvider>();
        private readonly IQueueProvider _queue = A.Fake<IQueueProvider>();
        private readonly IDateTimeProvider _clock = A.Fake<IDateTimeProvider>();
        private readonly IGreyList _greylist = new GreyList(new LoggerFactory(), new DateTimeProvider());
        private readonly WorkflowOptions _options = new WorkflowOptions(A.Fake<IServiceCollection>());

        public UnmatchedEventFixture()
        {
            A.CallTo(() => _clock.UtcNow).Returns(Now);
            A.CallTo(() => _clock.Now).Returns(Now);
            A.CallTo(() => _persistence.GetSubscriptions(A<string>._, A<string>._, A<DateTime>._, A<CancellationToken>._))
                .Returns(new List<EventSubscription>());
        }

        private void GivenEventAged(TimeSpan age) =>
            A.CallTo(() => _persistence.GetEvent(EventId, A<CancellationToken>._))
                .Returns(new Event { Id = EventId, EventName = "e", EventKey = "k", EventTime = Now - age, IsProcessed = false });

        private TestableEventConsumer Consumer() => new TestableEventConsumer(_persistence, _queue, _options, _clock, _greylist);

        [Fact(DisplayName = "Unmatched event within retention stays unprocessed and is greylisted")]
        public async Task unmatched_within_retention()
        {
            GivenEventAged(TimeSpan.FromMinutes(1));
            await Consumer().Process(EventId);

            A.CallTo(() => _persistence.MarkEventProcessed(EventId, A<CancellationToken>._)).MustNotHaveHappened();
            Assert.True(_greylist.Contains($"evt:{EventId}"));
        }

        [Fact(DisplayName = "Unmatched event older than retention is marked processed")]
        public async Task unmatched_past_retention()
        {
            GivenEventAged(TimeSpan.FromMinutes(6));
            await Consumer().Process(EventId);

            A.CallTo(() => _persistence.MarkEventProcessed(EventId, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "Zero retention marks unmatched events processed immediately (3.21 behaviour)")]
        public async Task zero_retention()
        {
            _options.UseUnmatchedEventRetention(TimeSpan.Zero);
            GivenEventAged(TimeSpan.Zero);
            await Consumer().Process(EventId);

            A.CallTo(() => _persistence.MarkEventProcessed(EventId, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "Negative retention is rejected")]
        public void negative_retention()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _options.UseUnmatchedEventRetention(TimeSpan.FromSeconds(-1)));
        }

        [Theory(DisplayName = "Poller does not reschedule a greylisted event on later polls")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task poller_respects_greylist(bool supportsScheduledCommands)
        {
            A.CallTo(() => _persistence.SupportsScheduledCommands).Returns(supportsScheduledCommands);
            A.CallTo(() => _persistence.GetRunnableEvents(A<DateTime>._, A<CancellationToken>._)).Returns(new[] { EventId });
            var poller = new RunnablePoller(_persistence, _queue, new LoggerFactory(), A.Fake<IServiceProvider>(), A.Fake<IWorkflowRegistry>(),
                new SingleNodeLockProvider(), _greylist, _clock, _options);

            for (var i = 0; i < 10; i++)
                await poller.PollEvents();

            var scheduled = supportsScheduledCommands
                ? A.CallTo(() => _persistence.ScheduleCommand(A<ScheduledCommand>._))
                : A.CallTo(() => _queue.QueueWork(EventId, QueueType.Event));
            scheduled.MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "Transient processing failure clears the greylist entry so the next poll retries")]
        public async Task failure_clears_greylist()
        {
            _greylist.Add($"evt:{EventId}");
            A.CallTo(() => _persistence.GetEvent(EventId, A<CancellationToken>._)).Throws(new TimeoutException("db down"));

            await Assert.ThrowsAsync<TimeoutException>(() => Consumer().Process(EventId));

            Assert.False(_greylist.Contains($"evt:{EventId}"));
        }

        [Fact(DisplayName = "Subscription lookup failure clears the greylist entry")]
        public async Task subscription_failure_clears_greylist()
        {
            _greylist.Add($"evt:{EventId}");
            GivenEventAged(TimeSpan.FromMinutes(1));
            A.CallTo(() => _persistence.GetSubscriptions(A<string>._, A<string>._, A<DateTime>._, A<CancellationToken>._))
                .Throws(new TimeoutException("db down"));

            await Assert.ThrowsAsync<TimeoutException>(() => Consumer().Process(EventId));

            Assert.False(_greylist.Contains($"evt:{EventId}"));
        }

        [Fact(DisplayName = "Unmatched event polled by a scheduled-command provider stays greylisted within retention")]
        public async Task unmatched_stays_greylisted_after_poll()
        {
            A.CallTo(() => _persistence.SupportsScheduledCommands).Returns(true);
            A.CallTo(() => _persistence.GetRunnableEvents(A<DateTime>._, A<CancellationToken>._)).Returns(new[] { EventId });
            GivenEventAged(TimeSpan.FromMinutes(1));
            var poller = new RunnablePoller(_persistence, _queue, new LoggerFactory(), A.Fake<IServiceProvider>(), A.Fake<IWorkflowRegistry>(),
                new SingleNodeLockProvider(), _greylist, _clock, _options);

            await poller.PollEvents();
            await Consumer().Process(EventId);
            await poller.PollEvents();

            Assert.True(_greylist.Contains($"evt:{EventId}"));
            A.CallTo(() => _persistence.ScheduleCommand(A<ScheduledCommand>._)).MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "After a processing failure a scheduled-command provider reschedules the event on the next poll")]
        public async Task failure_reschedules_on_next_poll()
        {
            A.CallTo(() => _persistence.SupportsScheduledCommands).Returns(true);
            A.CallTo(() => _persistence.GetRunnableEvents(A<DateTime>._, A<CancellationToken>._)).Returns(new[] { EventId });
            A.CallTo(() => _persistence.GetEvent(EventId, A<CancellationToken>._)).Throws(new TimeoutException("db down"));
            var poller = new RunnablePoller(_persistence, _queue, new LoggerFactory(), A.Fake<IServiceProvider>(), A.Fake<IWorkflowRegistry>(),
                new SingleNodeLockProvider(), _greylist, _clock, _options);

            await poller.PollEvents();
            await Assert.ThrowsAsync<TimeoutException>(() => Consumer().Process(EventId));
            await poller.PollEvents();

            A.CallTo(() => _persistence.ScheduleCommand(A<ScheduledCommand>._)).MustHaveHappenedTwiceExactly();
        }

        private class TestableEventConsumer : EventConsumer
        {
            public TestableEventConsumer(IPersistenceProvider p, IQueueProvider q, WorkflowOptions o, IDateTimeProvider c, IGreyList g)
                : base(p, p, p, q, new LoggerFactory(), A.Fake<IServiceProvider>(), A.Fake<IWorkflowRegistry>(), new SingleNodeLockProvider(), o, c, g)
            { }

            public Task Process(string id) => ProcessItem(id, CancellationToken.None);
        }
    }
}
