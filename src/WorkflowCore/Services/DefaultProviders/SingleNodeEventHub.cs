using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class SingleNodeEventHub : ILifeCycleEventHub
    {
        private ICollection<Action<LifeCycleEvent>> _subscribers = new HashSet<Action<LifeCycleEvent>>();
        private readonly ILogger _logger;

        public SingleNodeEventHub(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SingleNodeEventHub>();
        }

        public Task PublishNotification(LifeCycleEvent evt)
        {
            Task.Run(() =>
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
            });
            return Task.CompletedTask;
        }

        public void Subscribe(Action<LifeCycleEvent> action)
        {
            _subscribers.Add(action);
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _subscribers.Clear();
            return Task.CompletedTask;
        }
    }
}
