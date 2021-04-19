using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisLifeCycleEventHub : ILifeCycleEventHub
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _channel;
        private ICollection<Action<LifeCycleEvent>> _subscribers = new HashSet<Action<LifeCycleEvent>>();
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        private IConnectionMultiplexer _multiplexer;
        private ISubscriber _subscriber;

        public RedisLifeCycleEventHub(string connectionString, string channel, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _channel = channel;
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task PublishNotification(LifeCycleEvent evt)
        {
            if (_subscriber == null)
                throw new InvalidOperationException();

            var data = JsonConvert.SerializeObject(evt, _serializerSettings);
            await _subscriber.PublishAsync(_channel, data);
        }

        public void Subscribe(Action<LifeCycleEvent> action)
        {
            _subscribers.Add(action);
        }

        public async Task Start()
        {
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            _subscriber = _multiplexer.GetSubscriber();
            _subscriber.Subscribe(_channel, (channel, message) => {
                var evt = JsonConvert.DeserializeObject(message, _serializerSettings);
                NotifySubscribers((LifeCycleEvent)evt);
            });
        }

        public async Task Stop()
        {
            await _subscriber.UnsubscribeAllAsync();
            await _multiplexer.CloseAsync();
            _subscriber = null;
            _multiplexer = null;
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
