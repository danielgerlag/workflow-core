using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    class EventTask : IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private readonly Task _task;
        private readonly IDateTimeProvider _datetimeProvider;
        private bool _shutdown = true;

        public EventTask(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options, IDateTimeProvider datetimeProvider)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<EventTask>();
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
            _task = new Task(RunEvents);
        }

        public void Start()
        {
            _shutdown = false;
            _task.Start();
        }

        public void Stop()
        {
            _shutdown = true;
            _task.Wait();
        }

        private async void RunEvents()
        {            
            while (!_shutdown)
            {
                try
                {
                    var eventId = await _queueProvider.DequeueWork(QueueType.Event);
                    if (eventId != null)
                        Parallel.Invoke(() => ProcessEvent(eventId));
                    else
                        await Task.Delay(_options.IdleTime); //no work
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        private async void ProcessEvent(string eventId)
        {
            try
            {
                if (await _lockProvider.AcquireLock($"evt:{eventId}"))
                {
                    try
                    {
                        var evt = await _persistenceStore.GetEvent(eventId);
                        if (evt.EventTime <= _datetimeProvider.Now.ToUniversalTime())
                        {
                            var subs = await _persistenceStore.GetSubcriptions(evt.EventName, evt.EventKey, evt.EventTime);
                            var success = true;

                            foreach (var sub in subs.ToList())
                                success = success && await SeedSubscription(evt, sub);

                            if (success)
                                await _persistenceStore.MarkEventProcessed(eventId);
                        }
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock($"evt:{eventId}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Event locked {eventId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task<bool> SeedSubscription(Event evt, EventSubscription sub)
        {
            if (await _lockProvider.AcquireLock(sub.WorkflowId))
            {
                try
                {
                    var workflow = await _persistenceStore.GetWorkflowInstance(sub.WorkflowId);
                    var pointers = workflow.ExecutionPointers.Where(p => p.EventName == sub.EventName && p.EventKey == sub.EventKey && !p.EventPublished);
                    foreach (var p in pointers)
                    {
                        p.EventData = evt.EventData;
                        p.EventPublished = true;
                        p.Active = true;
                    }
                    workflow.NextExecution = 0;
                    await _persistenceStore.PersistWorkflow(workflow);
                    await _persistenceStore.TerminateSubscription(sub.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return false;
                }
                finally
                {
                    await _lockProvider.ReleaseLock(sub.WorkflowId);
                    await _queueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
                }
            }
            else
            {
                _logger.LogInformation("Workflow locked {0}", sub.WorkflowId);
                return false;
            }
        }
    }
}
