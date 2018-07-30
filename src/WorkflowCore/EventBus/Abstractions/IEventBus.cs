using System;
using System.Threading.Tasks;

namespace WorkflowCore.EventBus.Abstractions
{
    public interface IEventBus
    {
        Task HandleEventAndPublish<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : IntegrationEvent;

        Task Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        Task Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }
}
