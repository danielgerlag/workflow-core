using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    class RunnablePoller : IRunnablePoller
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private Timer _pollTimer;

        public RunnablePoller(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;            
            _logger = loggerFactory.CreateLogger<RunnablePoller>();
            _lockProvider = lockProvider;
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
        private void PollRunnables(object target)
        {
            try
            {
                if (_lockProvider.AcquireLock("poll runnables").Result)
                {
                    try
                    {
                        _logger.LogInformation("Polling for runnable workflows");                        
                        var runnables = _persistenceStore.GetRunnableInstances().Result;
                        foreach (var item in runnables)
                        {
                            _logger.LogDebug("Got runnable instance {0}", item);
                            _queueProvider.QueueForProcessing(item);
                        }
                    }
                    finally
                    {
                        _lockProvider.ReleaseLock("poll runnables").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            try
            {
                if (_lockProvider.AcquireLock("unpublished events").Result)
                {
                    try
                    {
                        _logger.LogInformation("Polling for unpublished events");                        
                        var events = _persistenceStore.GetUnpublishedEvents().Result.ToList();
                        foreach (var item in events)
                        {
                            _logger.LogDebug("Got unpublished event {0} {1}", item.EventName, item.EventKey);
                            _queueProvider.QueueForPublishing(item).Wait();
                            _persistenceStore.RemoveUnpublishedEvent(item.Id).Wait();
                        }
                    }
                    finally
                    {
                        _lockProvider.ReleaseLock("unpublished events").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
