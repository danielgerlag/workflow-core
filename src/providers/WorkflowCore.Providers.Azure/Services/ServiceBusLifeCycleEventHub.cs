using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Providers.Azure.Services
{
    public class ServiceBusLifeCycleEventHub : ILifeCycleEventHub
    {
        private readonly ILogger _logger;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusReceiver _receiver;
        private readonly ServiceBusProcessor _processor;

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
            var client = new ServiceBusClient(connectionString);
            _sender = client.CreateSender(topicName);
            _receiver = client.CreateReceiver(topicName, subscriptionName);
            _processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false
            });
            _logger = logFactory.CreateLogger(GetType());
        }

        public ServiceBusLifeCycleEventHub(
            string fullyQualifiedNamespace,
            TokenCredential tokenCredential,
            string topicName,
            string subscriptionName,
            ILoggerFactory logFactory)
        {
            var client = new ServiceBusClient(fullyQualifiedNamespace, tokenCredential);
            _sender = client.CreateSender(topicName);
            _receiver = client.CreateReceiver(topicName, subscriptionName);
            _processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false
            });
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task PublishNotification(LifeCycleEvent evt)
        {
            var payload = JsonConvert.SerializeObject(evt, _serializerSettings);
            var message = new ServiceBusMessage(payload);
            await _sender.SendMessageAsync(message);
        }

        public void Subscribe(Action<LifeCycleEvent> action)
        {
            _subscribers.Add(action);
        }

        public async Task Start()
        {
            _processor.ProcessErrorAsync += ExceptionHandler;
            _processor.ProcessMessageAsync += MessageHandler;
            await _processor.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await _sender.CloseAsync();
            await _receiver.CloseAsync();
            await _processor.CloseAsync();
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                var payload = args.Message.Body.ToString();
                var evt = JsonConvert.DeserializeObject<LifeCycleEvent>(
                    payload, _serializerSettings);

                NotifySubscribers(evt);

                await _receiver.CompleteMessageAsync(args.Message);
            }
            catch
            {
                await _receiver.AbandonMessageAsync(args.Message);
            }
        }

        private Task ExceptionHandler(ProcessErrorEventArgs arg)
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
