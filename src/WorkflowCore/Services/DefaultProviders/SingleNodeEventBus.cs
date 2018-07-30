using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using WorkflowCore.EventBus;
using WorkflowCore.EventBus.Abstractions;

namespace WorkflowCore.Services
{
    public class SingleNodeEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBusSubscriptionsManager _subsManager;

        public SingleNodeEventBus(IEventBusSubscriptionsManager subsManager, IServiceProvider serviceProvider)
        {
            _subsManager = subsManager;
            _serviceProvider = serviceProvider;
        }

        public Task HandleEventAndPublish<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : IntegrationEvent
        {
            return HandleEvent<TIntegrationEvent>(@event);

            // No need to publish in single node
        }

        private async Task HandleEvent<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : IntegrationEvent
        {
            string eventName = _subsManager.GetEventKey(@event.GetType());
            IEnumerable<SubscriptionInfo> subscriptions = _subsManager.GetHandlersForEvent(eventName);
            foreach (var subscription in subscriptions)
            {
                IIntegrationEventHandler<TIntegrationEvent> handler = (IIntegrationEventHandler<TIntegrationEvent>)_serviceProvider.GetService(subscription.HandlerType);

                await handler.Handle(@event);
            }
        }

        public Task Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            _subsManager.AddSubscription<T, TH>();

            return Task.CompletedTask;
        }

        public Task Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            _subsManager.RemoveSubscription<T, TH>();

            return Task.CompletedTask;
        }
    }
}
