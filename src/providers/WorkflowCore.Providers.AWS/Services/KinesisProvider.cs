using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Providers.AWS.Interface;

namespace WorkflowCore.Providers.AWS.Services
{
    public class KinesisProvider : ILifeCycleEventHub
    {
        private readonly ILogger _logger;
        private Queue<Action<LifeCycleEvent>> _deferredSubscribers = new Queue<Action<LifeCycleEvent>>();
        private readonly string _streamName;
        private readonly string _appName;
        private readonly JsonSerializer _serializer;
        private readonly IKinesisStreamConsumer _consumer;
        private readonly AmazonKinesisClient _client;
        private readonly int _defaultShardCount = 1;
        private bool _started = false;

        public KinesisProvider(AmazonKinesisClient kinesisClient, string appName, string streamName, IKinesisStreamConsumer consumer, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger(GetType());
            _appName = appName;
            _streamName = streamName;
            _consumer = consumer;
            _serializer = new JsonSerializer();            
            _serializer.TypeNameHandling = TypeNameHandling.All;
            _client = kinesisClient;
        }

        public async Task PublishNotification(LifeCycleEvent evt)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);                
                _serializer.Serialize(writer, evt);
                writer.Flush();

                var response = await _client.PutRecordAsync(new PutRecordRequest
                {
                    StreamName = _streamName,
                    PartitionKey = evt.WorkflowInstanceId,
                    Data = stream
                });

                //if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                //{
                //    _logger.LogWarning($"Failed to send event to Kinesis {response.HttpStatusCode}");
                //}
            }
        }

        public void Subscribe(Action<LifeCycleEvent> action)
        {
            if (_started)
            {
                _consumer.Subscribe(_appName, _streamName, record => Consume(record, action));
            }
            else
            {
                _deferredSubscribers.Enqueue(action);
            }
        }

        public async Task Start()
        {
            await EnsureStream();
            _started = true;
            while (_deferredSubscribers.Count > 0)
            {
                var action = _deferredSubscribers.Dequeue();
                await _consumer.Subscribe(_appName, _streamName, record => Consume(record, action));
            }
        }

        public Task Stop()
        {
            _started = false;
            return Task.CompletedTask;
        }

        private async Task EnsureStream()
        {
            try
            {
                await _client.DescribeStreamSummaryAsync(new DescribeStreamSummaryRequest
                {
                    StreamName = _streamName
                });
            }
            catch (ResourceNotFoundException)
            {
                await CreateStream();
            }
        }

        private async Task<string> CreateStream()
        {
            await _client.CreateStreamAsync(new CreateStreamRequest
            {
                StreamName = _streamName,
                ShardCount = _defaultShardCount
            });
            
            var i = 0;
            while (i < 20)
            {
                i++;
                await Task.Delay(3000);
                var poll = await _client.DescribeStreamSummaryAsync(new DescribeStreamSummaryRequest
                {
                    StreamName = _streamName
                });
                
                if (poll.StreamDescriptionSummary.StreamStatus == StreamStatus.ACTIVE)
                    return poll.StreamDescriptionSummary.StreamARN;
            }

            throw new TimeoutException();
        }                

        private void Consume(Record record, Action<LifeCycleEvent> action)
        {
            using (var strm = new StreamReader(record.Data))
            {
                var evt = _serializer.Deserialize(new JsonTextReader(strm));
                action(evt as LifeCycleEvent);
            }
        }
    }
}
