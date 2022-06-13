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
            if (_outbox.IsAddingCompleted || !_workflowOptions.EnableLifeCycleEventsPublisher)
                return;

            _outbox.Add(evt);
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

            _dispatchTask = new Task(Execute);
            _dispatchTask.Start();
        }

        public void Stop()
        {
            _outbox.CompleteAdding();
            _dispatchTask.Wait();
            _dispatchTask = null;
        }

        public void Dispose()
        {
            _outbox.Dispose();
        }

        private async void Execute()
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
    }
}