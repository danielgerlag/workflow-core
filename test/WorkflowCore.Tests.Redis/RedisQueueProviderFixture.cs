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
    public abstract class RedisQueueProviderFixture : IAsyncLifetime
    {
        private readonly string _prefix = "q-" + Guid.NewGuid().ToString("N");
        private IConnectionMultiplexer _mux;

        protected abstract RedisQueueStorage Storage { get; }

        protected RedisQueueProvider Subject { get; private set; }
        protected IDatabase Db { get; private set; }

        protected RedisQueueProviderFixture(RedisDockerSetup dockerSetup)
        {
            if (dockerSetup == null)
                throw new ArgumentNullException(nameof(dockerSetup));
        }

        public async Task InitializeAsync()
        {
            Subject = CreateProvider();
            await Subject.Start();
            _mux = await ConnectionMultiplexer.ConnectAsync(RedisDockerSetup.ConnectionString);
            Db = _mux.GetDatabase();
        }

        public async Task DisposeAsync()
        {
            if (Subject != null)
                await Subject.Stop();
            if (_mux != null)
                await _mux.CloseAsync();
        }

        [Fact]
        public async Task QueueWork_when_id_already_pending_is_noop()
        {
            await Subject.QueueWork("a", QueueType.Workflow);
            await Subject.QueueWork("b", QueueType.Workflow);
            await Subject.QueueWork("a", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("a", "b");
        }

        [Fact]
        public async Task QueueWork_concurrent_missing_id_leaves_one_pending_entry()
        {
            const string id = "same-id";
            var tasks = Enumerable.Range(0, 50)
                .Select(_ => Subject.QueueWork(id, QueueType.Workflow));

            await Task.WhenAll(tasks);

            (await Pending(QueueType.Workflow)).Should().Equal(id);
        }

        [Fact]
        public async Task QueueWork_preserves_fifo_for_distinct_ids()
        {
            await Subject.QueueWork("first", QueueType.Event);
            await Subject.QueueWork("second", QueueType.Event);
            await Subject.QueueWork("third", QueueType.Event);

            (await Pending(QueueType.Event)).Should().Equal("first", "second", "third");
        }

        [Fact]
        public async Task QueueWork_writes_selected_storage_type()
        {
            await Subject.QueueWork("typed", QueueType.Workflow);

            var expected = Storage == RedisQueueStorage.SortedSet
                ? RedisType.SortedSet
                : RedisType.List;
            (await KeyType(QueueKey(QueueType.Workflow))).Should().Be(expected);
        }

        [Fact]
        public async Task DequeueWork_returns_fifo_and_then_null()
        {
            await Subject.QueueWork("first", QueueType.Event);
            await Subject.QueueWork("second", QueueType.Event);

            (await Subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().Be("first");
            (await Subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().Be("second");
            (await Subject.DequeueWork(QueueType.Event, CancellationToken.None)).Should().BeNull();
            (await Pending(QueueType.Event)).Should().BeEmpty();
        }

        [Fact]
        public async Task DequeueWork_allows_id_to_be_queued_again()
        {
            await Subject.QueueWork("recycle", QueueType.Index);
            (await Subject.DequeueWork(QueueType.Index, CancellationToken.None)).Should().Be("recycle");

            await Subject.QueueWork("recycle", QueueType.Index);

            (await Pending(QueueType.Index)).Should().Equal("recycle");
        }

        [Fact]
        public async Task DequeueWork_then_reenqueue_uses_new_fifo_position()
        {
            await Subject.QueueWork("recycle", QueueType.Workflow);
            await Subject.QueueWork("other", QueueType.Workflow);
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("recycle");

            await Subject.QueueWork("recycle", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("other", "recycle");
        }

        [Fact]
        public async Task DequeueWork_concurrent_pops_each_unique_id_once()
        {
            var ids = Enumerable.Range(0, 40).Select(i => $"id-{i}").ToArray();
            foreach (var id in ids)
                await Subject.QueueWork(id, QueueType.Workflow);

            var dequeued = new ConcurrentBag<string>();
            var workers = Enumerable.Range(0, 8).Select(async _ =>
            {
                while (true)
                {
                    var item = await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None);
                    if (item == null)
                        return;
                    dequeued.Add(item);
                }
            });

            await Task.WhenAll(workers);

            dequeued.Should().BeEquivalentTo(ids);
            (await Pending(QueueType.Workflow)).Should().BeEmpty();
        }

        protected RedisQueueProvider CreateProvider()
        {
            return new RedisQueueProvider(RedisDockerSetup.ConnectionString, _prefix, Storage, new LoggerFactory());
        }

        protected async Task RestartSubject(RedisQueueProvider provider)
        {
            if (Subject != null)
                await Subject.Stop();

            Subject = provider;
            if (Subject != null)
                await Subject.Start();
        }

        protected string QueueKey(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow: return $"{_prefix}-workflows";
                case QueueType.Event: return $"{_prefix}-events";
                case QueueType.Index: return $"{_prefix}-index";
                default: throw new ArgumentOutOfRangeException(nameof(queue));
            }
        }

        protected async Task<List<string>> Pending(QueueType queue)
        {
            if (Storage == RedisQueueStorage.SortedSet)
            {
                var zset = await Db.SortedSetRangeByRankAsync(QueueKey(queue));
                return zset.Select(v => (string)v).ToList();
            }

            var list = await Db.ListRangeAsync(QueueKey(queue));
            return list.Select(v => (string)v).ToList();
        }

        protected Task<RedisType> KeyType(string key) => Db.KeyTypeAsync(key);
    }

    [Collection("Redis collection")]
    public class RedisQueueProviderListFixture : RedisQueueProviderFixture
    {
        public RedisQueueProviderListFixture(RedisDockerSetup dockerSetup)
            : base(dockerSetup)
        {
        }

        protected override RedisQueueStorage Storage => RedisQueueStorage.List;

        [Fact]
        public async Task Default_constructor_uses_list_storage()
        {
            var key = QueueKey(QueueType.Index);
            var prefix = key.Substring(0, key.LastIndexOf('-'));
            var subject = new RedisQueueProvider(RedisDockerSetup.ConnectionString, prefix, new LoggerFactory());
            await subject.Start();
            try
            {
                await subject.QueueWork("default-list", QueueType.Index);
                (await KeyType(key)).Should().Be(RedisType.List);
            }
            finally
            {
                await subject.Stop();
            }
        }

        [Fact]
        public async Task QueueWork_does_not_add_to_preexisting_duplicate_list_entries()
        {
            var key = QueueKey(QueueType.Workflow);
            await Db.ListRightPushAsync(key, new RedisValue[] { "dup", "dup", "dup" });

            await Subject.QueueWork("dup", QueueType.Workflow);

            (await Pending(QueueType.Workflow)).Should().Equal("dup", "dup", "dup");
        }

        [Fact]
        public async Task DequeueWork_drains_preexisting_duplicates_then_empty()
        {
            var key = QueueKey(QueueType.Workflow);
            await Db.ListRightPushAsync(key, new RedisValue[] { "legacy", "legacy" });

            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("legacy");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("legacy");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().BeNull();
        }

        [Fact]
        public async Task Start_does_not_migrate_existing_list()
        {
            var key = QueueKey(QueueType.Workflow);
            await Subject.QueueWork("keep", QueueType.Workflow);
            await RestartSubject(CreateProvider());

            (await KeyType(key)).Should().Be(RedisType.List);
            (await Pending(QueueType.Workflow)).Should().Equal("keep");
        }
    }

    [Collection("Redis collection")]
    public class RedisQueueProviderSortedSetFixture : RedisQueueProviderFixture
    {
        public RedisQueueProviderSortedSetFixture(RedisDockerSetup dockerSetup)
            : base(dockerSetup)
        {
        }

        protected override RedisQueueStorage Storage => RedisQueueStorage.SortedSet;

        [Fact]
        public async Task Start_migrates_legacy_list_keeping_first_occurrence_and_fifo()
        {
            var key = QueueKey(QueueType.Workflow);
            await RestartSubject(null);
            await Db.KeyDeleteAsync(key);
            await Db.ListRightPushAsync(key, new RedisValue[] { "first", "second", "first", "third" });

            await RestartSubject(CreateProvider());

            (await KeyType(key)).Should().Be(RedisType.SortedSet);
            (await Pending(QueueType.Workflow)).Should().Equal("first", "second", "third");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("first");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("second");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().Be("third");
            (await Subject.DequeueWork(QueueType.Workflow, CancellationToken.None)).Should().BeNull();
        }

        [Fact]
        public async Task Start_leaves_existing_zset_untouched()
        {
            await Subject.QueueWork("keep", QueueType.Index);
            await RestartSubject(CreateProvider());

            (await KeyType(QueueKey(QueueType.Index))).Should().Be(RedisType.SortedSet);
            (await Pending(QueueType.Index)).Should().Equal("keep");
        }

        [Fact]
        public async Task QueueWork_after_list_migrate_appends_behind_migrated_items()
        {
            var key = QueueKey(QueueType.Event);
            await RestartSubject(null);
            await Db.KeyDeleteAsync(key);
            await Db.ListRightPushAsync(key, new RedisValue[] { "legacy" });

            await RestartSubject(CreateProvider());
            await Subject.QueueWork("fresh", QueueType.Event);

            (await Pending(QueueType.Event)).Should().Equal("legacy", "fresh");
        }
    }
}
