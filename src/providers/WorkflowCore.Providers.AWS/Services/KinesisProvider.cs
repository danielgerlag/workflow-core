using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private ICollection<Action<LifeCycleEvent>> _subscribers = new HashSet<Action<LifeCycleEvent>>();
        private readonly string _streamName;
        private readonly string _appName;
        private readonly JsonSerializer _serializer;
        private readonly IKinesisStreamConsumer _consumer;

        private AmazonKinesisClient _client;
        private readonly int _defaultShardCount = 1;

        public KinesisProvider(AWSCredentials credentials, RegionEndpoint region, string appName, string streamName, IKinesisStreamConsumer consumer, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger(GetType());
            _streamName = streamName;
            _consumer = consumer;
            _serializer = new JsonSerializer();
        }

        public async Task PublishNotification(LifeCycleEvent evt)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                _serializer.Serialize(writer, evt);
                writer.Flush();

                var response = await _client.PutRecordAsync(new PutRecordRequest()
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
            _consumer.Subscribe(_appName, _streamName, record =>
            {
                using (var strm = new StreamReader(record.Data))
                {
                    var evt = _serializer.Deserialize<LifeCycleEvent>(new JsonTextReader(strm));
                    action(evt);
                }
            });
        }

        public async Task Start()
        {
            await EnsureStream();
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        private async Task EnsureStream()
        {
            try
            {
                await _client.DescribeStreamSummaryAsync(new DescribeStreamSummaryRequest()
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
            await _client.CreateStreamAsync(new CreateStreamRequest()
            {
                StreamName = _streamName,
                ShardCount = _defaultShardCount
            });
            
            var i = 0;
            while (i < 20)
            {
                i++;
                await Task.Delay(3000);
                var poll = await _client.DescribeStreamSummaryAsync(new DescribeStreamSummaryRequest()
                {
                    StreamName = _streamName
                });
                
                if (poll.StreamDescriptionSummary.StreamStatus == StreamStatus.ACTIVE)
                    return poll.StreamDescriptionSummary.StreamARN;
            }

            throw new TimeoutException();
        }

        private void NotifySubscribers(LifeCycleEvent evt)
        {
            foreach (var subscriber in _subscribers)
            {
                try
                {
                    subscriber(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(default(EventId), ex, $"Error on event subscriber: {ex.Message}");
                }
            }
        }
    }
}
