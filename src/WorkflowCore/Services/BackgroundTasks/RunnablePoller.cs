using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class RunnablePoller : IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;
        private Timer _pollTimer;

        public RunnablePoller(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IDateTimeProvider dateTimeProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;            
            _logger = loggerFactory.CreateLogger<RunnablePoller>();
            _lockProvider = lockProvider;
            _dateTimeProvider = dateTimeProvider;
            _options = options;
        }

        public void Start()
        {
            _pollTimer = new Timer(new TimerCallback(PollRunnables), null, TimeSpan.FromSeconds(0), _options.PollInterval);
        }

        public void Stop()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        /// <summary>
        /// Poll the persistence store for workflows ready to run.
        /// Poll the persistence store for stashed unpublished events
        /// </summary>        
        private async void PollRunnables(object target)
        {
            try
            {
                _logger.LogInformation("Polling for runnable workflows");
                var runnables = await _persistenceStore.GetRunnableInstances(_dateTimeProvider.Now);
                foreach (var item in runnables)
                {
                    if (await _lockProvider.AcquireLock($"wf:{item}", default))
                    {
                        _logger.LogDebug("Got runnable instance {0}", item);
                        await _queueProvider.QueueWork(item, QueueType.Workflow);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            try
            {
                _logger.LogInformation("Polling for unprocessed events");
                var events = await _persistenceStore.GetRunnableEvents(_dateTimeProvider.Now);
                foreach (var item in events.ToList())
                {
                    if (await _lockProvider.AcquireLock($"evt:{item}", default))
                    {
                        _logger.LogDebug($"Got unprocessed event {item}");
                        await _queueProvider.QueueWork(item, QueueType.Event);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
