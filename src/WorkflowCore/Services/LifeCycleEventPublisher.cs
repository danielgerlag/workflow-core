using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class LifeCycleEventPublisher : ILifeCycleEventPublisher
    {
        private readonly ILifeCycleEventHub _eventHub;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<LifeCycleEvent> _outbox;
        protected Task DispatchTask;
        private CancellationTokenSource _cancellationTokenSource;

        public LifeCycleEventPublisher(ILifeCycleEventHub eventHub, ILoggerFactory loggerFactory)
        {
            _eventHub = eventHub;
            _outbox = new ConcurrentQueue<LifeCycleEvent>();
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public void PublishNotification(LifeCycleEvent evt)
        {
            _outbox.Enqueue(evt);
        }

        public void Start()
        {
            if (DispatchTask != null)
            {
                throw new InvalidOperationException();
            }

            _cancellationTokenSource = new CancellationTokenSource();

            DispatchTask = new Task(Execute);
            DispatchTask.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            DispatchTask.Wait();
            DispatchTask = null;
        }

        private async void Execute()
        {
            var cancelToken = _cancellationTokenSource.Token;
            
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    if (!SpinWait.SpinUntil(() => _outbox.Count > 0, 1000))
                    {
                        continue;
                    }

                    if (_outbox.TryDequeue(out LifeCycleEvent evt))
                    {
                        await _eventHub.PublishNotification(evt);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }
    }
}
