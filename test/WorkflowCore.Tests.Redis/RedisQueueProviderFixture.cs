using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Redis.Services;
using Xunit;

namespace WorkflowCore.Tests.Redis
{
    [Collection("Redis collection")]
    public class RedisQueueProviderFixture : IAsyncLifetime
    {
        private readonly string _prefix = "q-" + Guid.NewGuid().ToString("N");
        private RedisQueueProvider _subject;
        private IConnectionMultiplexer _mux;
        private IDatabase _db;

        public RedisQueueProviderFixture(RedisDockerSetup dockerSetup)
        {
            if (dockerSetup == null)
                throw new ArgumentNullException(nameof(dockerSetup));
        }

        public async Task InitializeAsync()
        {
            _subject = new RedisQueueProvider(RedisDockerSetup.ConnectionString, _prefix, new LoggerFactory());
            await _subject.Start();
            _mux = await ConnectionMultiplexer.ConnectAsync(RedisDockerSetup.ConnectionString);
            _db = _mux.GetDatabase();
        }

        public async Task DisposeAsync()
        {
            if (_subject != null)
                await _subject.Stop();
            if (_mux != null)
                await _mux.CloseAsync();
        }

        [Fact]
        public async Task QueueWork_when_id_already_pending_is_noop()
        {
            await _subject.QueueWork("a", QueueType.Workflow);
            await _subject.QueueWork("b", QueueType.Workflow);
            await _subject.QueueWork("a", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("a", "b");
        }

        [Fact]
        public async Task QueueWork_concurrent_missing_id_leaves_one_pending_entry()
        {
            const string id = "same-id";
            var tasks = Enumerable.Range(0, 50)
                .Select(_ => _subject.QueueWork(id, QueueType.Workflow));

            await Task.WhenAll(tasks);

            (await Pending(QueueType.Workflow)).Should().Equal(id);
        }

        [Fact]
        public async Task QueueWork_does_not_add_to_preexisting_duplicate_list_entries()
        {
            var key = QueueKey(QueueType.Workflow);
            await _db.ListRightPushAsync(key, new RedisValue[] { "dup", "dup", "dup" });

            await _subject.QueueWork("dup", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("dup", "dup", "dup");
        }

        [Fact]
        public async Task DequeueWork_returns_fifo_and_then_null()
        {
            await _subject.QueueWork("first", QueueType.Event);
            await _subject.QueueWork("second", QueueType.Event);

            (await _subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().Be("first");
            (await _subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().Be("second");
            (await _subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().BeNull();
            (await Pending(QueueType.Event)).Should().BeEmpty();
        }

        [Fact]
        public async Task DequeueWork_allows_id_to_be_queued_again()
        {
            await _subject.QueueWork("recycle", QueueType.Index);
            (await _subject.DequeueWork(QueueType.Index, CancellationToken.None)).Should().Be("recycle");

            await _subject.QueueWork("recycle", QueueType.Index);

            (await Pending(QueueType.Index)).Should().Equal("recycle");
        }

        [Fact]
        public async Task DequeueWork_concurrent_pops_each_unique_id_once()
        {
            var ids = Enumerable.Range(0, 40).Select(i => $"id-{i}").ToArray();
            foreach (var id in ids)
                await _subject.QueueWork(id, QueueType.Workflow);

            var dequeued = new ConcurrentBag<string>();
            var workers = Enumerable.Range(0, 8).Select(async _ =>
            {
                while (true)
                {
                    var item = await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None);
                    if (item == null)
                        return;
                    dequeued.Add(item);
                }
            });

            await Task.WhenAll(workers);

            dequeued.Should().BeEquivalentTo(ids);
            (await Pending(QueueType.Workflow)).Should().BeEmpty();
        }

        [Fact]
        public async Task DequeueWork_drains_preexisting_duplicates_then_empty()
        {
            var key = QueueKey(QueueType.Workflow);
            await _db.ListRightPushAsync(key, new RedisValue[] { "legacy", "legacy" });

            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("legacy");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("legacy");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().BeNull();
        }

        private string QueueKey(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow: return $"{_prefix}-workflows";
                case QueueType.Event: return $"{_prefix}-events";
                case QueueType.Index: return $"{_prefix}-index";
                default: throw new ArgumentOutOfRangeException(nameof(queue));
            }
        }

        private async Task<List<string>> Pending(QueueType queue)
        {
            var values = await _db.ListRangeAsync(QueueKey(queue));
            return values.Select(v => (string)v).ToList();
        }
    }
}
