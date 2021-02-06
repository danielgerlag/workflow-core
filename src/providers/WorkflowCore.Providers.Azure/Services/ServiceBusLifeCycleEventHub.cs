using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Providers.Azure.Services
{
    public class ServiceBusLifeCycleEventHub : ILifeCycleEventHub
    {
        private readonly ITopicClient _topicClient;
        private readonly ILogger _logger;
        private readonly ISubscriptionClient _subscriptionClient;
        private readonly ICollection<Action<LifeCycleEvent>> _subscribers = new HashSet<Action<LifeCycleEvent>>();
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
        };

        public ServiceBusLifeCycleEventHub(
            string connectionString,
            string topicName,
            string subscriptionName,
            ILoggerFactory logFactory)
        {
            _subscriptionClient = new SubscriptionClient(connectionString, topicName, subscriptionName);
            _topicClient = new TopicClient(connectionString, topicName);
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task PublishNotification(LifeCycleEvent evt)
        {
            var payload = JsonConvert.SerializeObject(evt, _serializerSettings);
            var message = new Message(Encoding.Default.GetBytes(payload))
            {
                Label = evt.Reference
            };

            await _topicClient.SendAsync(message);
        }

        public void Subscribe(Action<LifeCycleEvent> action)
        {
            _subscribers.Add(action);
        }

        public Task Start()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionHandler)
            {
                AutoComplete = false
            };

            _subscriptionClient.RegisterMessageHandler(MessageHandler, messageHandlerOptions);

            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            await _topicClient.CloseAsync();
            await _subscriptionClient.CloseAsync();
        }

        private async Task MessageHandler(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var payload = Encoding.Default.GetString(message.Body);
                var evt = JsonConvert.DeserializeObject<LifeCycleEvent>(
                    payload, _serializerSettings);

                NotifySubscribers(evt);

                await _subscriptionClient
                    .CompleteAsync(message.SystemProperties.LockToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await _subscriptionClient.AbandonAsync(message.SystemProperties.LockToken);
            }
        }

        private Task ExceptionHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogWarning(default, arg.Exception, "Error on receiving events");

            return Task.CompletedTask;
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
                    _logger.LogWarning(
                        default, ex, $"Error on event subscriber: {ex.Message}");
                }
            }
        }
    }
}
