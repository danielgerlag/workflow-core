using System;
using System.Linq;
using System.Threading;
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
        private readonly IGreyList _greylist;
        private readonly WorkflowOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;
        private Timer _pollTimer;

        public RunnablePoller(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IGreyList greylist, IDateTimeProvider dateTimeProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _greylist = greylist;
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
                if (await _lockProvider.AcquireLock("poll runnables", new CancellationToken()))
                {
                    try
                    {
                        _logger.LogInformation("Polling for runnable workflows");                        
                        var runnables = await _persistenceStore.GetRunnableInstances(_dateTimeProvider.Now);
                        foreach (var item in runnables)
                        {
                            if (_greylist.Contains($"wf:{item}"))
                            {
                                _logger.LogDebug($"Got greylisted workflow {item}");
                                continue;
                            }
                            _logger.LogDebug("Got runnable instance {0}", item);
                            _greylist.Add($"wf:{item}");
                            await _queueProvider.QueueWork(item, QueueType.Workflow);
                        }
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock("poll runnables");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            try
            {
                if (await _lockProvider.AcquireLock("unprocessed events", new CancellationToken()))
                {
                    try
                    {
                        _logger.LogInformation("Polling for unprocessed events");                        
                        var events = await _persistenceStore.GetRunnableEvents(_dateTimeProvider.Now);
                        foreach (var item in events.ToList())
                        {
                            if (_greylist.Contains($"evt:{item}"))
                            {
                                _logger.LogDebug($"Got greylisted event {item}");
                                _greylist.Add($"evt:{item}");
                                continue;
                            }
                            _logger.LogDebug($"Got unprocessed event {item}");
                            _greylist.Add($"evt:{item}");
                            await _queueProvider.QueueWork(item, QueueType.Event);
                        }
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock("unprocessed events");
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
