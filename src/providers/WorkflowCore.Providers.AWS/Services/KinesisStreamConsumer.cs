using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Providers.AWS.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class KinesisStreamConsumer : IKinesisStreamConsumer, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IKinesisTracker _tracker;
        private readonly IDistributedLockProvider _lockManager;
        private readonly AmazonKinesisClient _client;
        private readonly CancellationTokenSource _cancelToken = new CancellationTokenSource();
        private readonly Task _processTask;
        private readonly int _batchSize = 100;
        private ICollection<ShardSubscription> _subscribers = new HashSet<ShardSubscription>();
        private readonly IDateTimeProvider _dateTimeProvider;
        
        public KinesisStreamConsumer(AWSCredentials credentials, RegionEndpoint region, IKinesisTracker tracker, IDistributedLockProvider lockManager, ILoggerFactory logFactory, IDateTimeProvider dateTimeProvider)
        {
            _logger = logFactory.CreateLogger(GetType());
            _tracker = tracker;
            _lockManager = lockManager;
            _client = new AmazonKinesisClient(credentials, region);
            _processTask = new Task(Process);
            _processTask.Start();
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Subscribe(string appName, string stream, Action<Record> action)
        {
            var shards = await _client.ListShardsAsync(new ListShardsRequest()
            {
                StreamName = stream
            });

            foreach (var shard in shards.Shards)
            {
                _subscribers.Add(new ShardSubscription()
                {
                    AppName = appName,
                    Stream = stream,
                    Shard = shard,
                    Action = action
                });
            }
        }

        private async void Process()
        {
            while (!_cancelToken.IsCancellationRequested)
            {
                try
                {
                    var todo = _subscribers.Where(x => x.Snooze < _dateTimeProvider.Now).ToList();
                    foreach (var sub in todo)
                    {
                        if (!await _lockManager.AcquireLock($"{sub.AppName}.{sub.Stream}.{sub.Shard.ShardId}",
                            _cancelToken.Token))
                            continue;

                        try
                        {
                            var records = await GetBatch(sub);

                            if (records.Records.Count == 0)
                                sub.Snooze = _dateTimeProvider.Now.AddSeconds(5);

                            var lastSequence = string.Empty;

                            foreach (var rec in records.Records)
                            {
                                lastSequence = rec.SequenceNumber;
                                try
                                {
                                    sub.Action(rec);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(default(EventId), ex, ex.Message);
                                }
                            }

                            if (lastSequence != string.Empty)
                                await _tracker.IncrementShardIteratorAndSequence(sub.AppName, sub.Stream, sub.Shard.ShardId, records.NextShardIterator, lastSequence);
                            else
                                await _tracker.IncrementShardIterator(sub.AppName, sub.Stream, sub.Shard.ShardId, records.NextShardIterator);
                        }
                        finally
                        {
                            await _lockManager.ReleaseLock($"{sub.AppName}.{sub.Stream}.{sub.Shard.ShardId}");
                        }
                    }

                    if (todo.Count == 0)
                        await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(default(EventId), ex, ex.Message);
                }
            }
        }

        private async Task<GetRecordsResponse> GetBatch(ShardSubscription sub)
        {
            var iterator = await _tracker.GetNextShardIterator(sub.AppName, sub.Stream, sub.Shard.ShardId);

            if (iterator == null)
            {
                var iterResp = await _client.GetShardIteratorAsync(new GetShardIteratorRequest()
                {
                    ShardId = sub.Shard.ShardId,
                    StreamName = sub.Stream,
                    ShardIteratorType = ShardIteratorType.AT_SEQUENCE_NUMBER,
                    StartingSequenceNumber = sub.Shard.SequenceNumberRange.StartingSequenceNumber
                });
                iterator = iterResp.ShardIterator;
            }

            try
            {
                var result = await _client.GetRecordsAsync(new GetRecordsRequest()
                {
                    ShardIterator = iterator,
                    Limit = _batchSize
                });

                return result;
            }
            catch (ExpiredIteratorException)
            {
                var lastSequence = await _tracker.GetNextLastSequenceNumber(sub.AppName, sub.Stream, sub.Shard.ShardId);
                var iterResp = await _client.GetShardIteratorAsync(new GetShardIteratorRequest()
                {
                    ShardId = sub.Shard.ShardId,
                    StreamName = sub.Stream,
                    ShardIteratorType = ShardIteratorType.AFTER_SEQUENCE_NUMBER,
                    StartingSequenceNumber = lastSequence
                });
                iterator = iterResp.ShardIterator;

                var result = await _client.GetRecordsAsync(new GetRecordsRequest()
                {
                    ShardIterator = iterator,
                    Limit = _batchSize
                });

                return result;
            }
        }
        
        public void Dispose()
        {
            _cancelToken.Cancel();
            _processTask.Wait(5000);
        }

        class ShardSubscription
        {
            public string AppName { get; set; }
            public string Stream { get; set; }
            public Shard Shard { get; set; }
            public Action<Record> Action { get; set; }
            public DateTime Snooze { get; set; } = DateTime.Now;
        }
    }
}
