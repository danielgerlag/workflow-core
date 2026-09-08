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
        public async Task QueueWork_preserves_fifo_for_distinct_ids()
        {
            await _subject.QueueWork("first", QueueType.Event);
            await _subject.QueueWork("second", QueueType.Event);
            await _subject.QueueWork("third", QueueType.Event);

            (await Pending(QueueType.Event)).Should().Equal("first", "second", "third");
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
        public async Task DequeueWork_then_reenqueue_uses_new_fifo_position()
        {
            await _subject.QueueWork("recycle", QueueType.Workflow);
            await _subject.QueueWork("other", QueueType.Workflow);
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("recycle");

            await _subject.QueueWork("recycle", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("other", "recycle");
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
        public async Task Start_migrates_legacy_list_keeping_first_occurrence_and_fifo()
        {
            var key = QueueKey(QueueType.Workflow);
            await _subject.Stop();
            await _db.KeyDeleteAsync(key);
            await _db.ListRightPushAsync(key, new RedisValue[] { "first", "second", "first", "third" });

            _subject = new RedisQueueProvider(RedisDockerSetup.ConnectionString, _prefix, new LoggerFactory());
            await _subject.Start();

            (await KeyType(key)).Should().Be(RedisType.SortedSet);
            (await Pending(QueueType.Workflow)).Should().Equal("first", "second", "third");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("first");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("second");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("third");
            (await _subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().BeNull();
        }

        [Fact]
        public async Task Start_leaves_existing_zset_untouched()
        {
            await _subject.QueueWork("keep", QueueType.Index);
            await _subject.Stop();

            _subject = new RedisQueueProvider(RedisDockerSetup.ConnectionString, _prefix, new LoggerFactory());
            await _subject.Start();

            (await KeyType(QueueKey(QueueType.Index))).Should().Be(RedisType.SortedSet);
            (await Pending(QueueType.Index)).Should().Equal("keep");
        }

        [Fact]
        public async Task QueueWork_after_list_migrate_appends_behind_migrated_items()
        {
            var key = QueueKey(QueueType.Event);
            await _subject.Stop();
            await _db.KeyDeleteAsync(key);
            await _db.ListRightPushAsync(key, new RedisValue[] { "legacy" });

            _subject = new RedisQueueProvider(RedisDockerSetup.ConnectionString, _prefix, new LoggerFactory());
            await _subject.Start();
            await _subject.QueueWork("fresh", QueueType.Event);

            (await Pending(QueueType.Event)).Should().Equal("legacy", "fresh");
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
            var values = await _db.SortedSetRangeByRankAsync(QueueKey(queue));
            return values.Select(v => (string)v).ToList();
        }

        private Task<RedisType> KeyType(string key) => _db.KeyTypeAsync(key);
    }
}
