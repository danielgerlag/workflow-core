using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class LifeCycleEventPublisher : ILifeCycleEventPublisher, IDisposable
    {
        private readonly ILifeCycleEventHub _eventHub;
        private readonly WorkflowOptions _workflowOptions;
        private readonly ILogger _logger;
        private BlockingCollection<LifeCycleEvent> _outbox;
        private Task _dispatchTask;

        public LifeCycleEventPublisher(ILifeCycleEventHub eventHub, WorkflowOptions workflowOptions, ILoggerFactory loggerFactory)
        {
            _eventHub = eventHub;
            _workflowOptions = workflowOptions;
            _outbox = new BlockingCollection<LifeCycleEvent>();
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public void PublishNotification(LifeCycleEvent evt)
        {
            if (!_workflowOptions.EnableLifeCycleEventsPublisher)
                return;

            try
            {
                _outbox.TryAdd(evt);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Start()
        {
            if (_dispatchTask != null)
            {
                throw new InvalidOperationException();
            }

            if (_outbox.IsAddingCompleted)
            {
                _outbox = new BlockingCollection<LifeCycleEvent>();
            }

            _dispatchTask = Task.Run(Execute);
        }

        public void Stop()
        {
            _outbox.CompleteAdding();
            _dispatchTask.Wait();
            _dispatchTask = null;
        }

        public void Dispose()
        {
            if (_dispatchTask != null)
            {
                if (!_outbox.IsAddingCompleted)
                    _outbox.CompleteAdding();
                if (!_dispatchTask.Wait(TimeSpan.FromSeconds(30)))
                    _logger.LogWarning("Lifecycle event publisher did not stop within timeout");
                _dispatchTask = null;
            }
            _outbox.Dispose();
        }

        private async Task Execute()
        {
            try
            {
                foreach (var evt in _outbox.GetConsumingEnumerable())
                {
                    try
                    {
                        await _eventHub.PublishNotification(evt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(default(EventId), ex, ex.Message);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}