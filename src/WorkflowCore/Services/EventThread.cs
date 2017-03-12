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
    class EventThread : IEventThread
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private bool _shutdown = true;
        private Thread _thread;

        public EventThread(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<EventThread>();
            _lockProvider = lockProvider;
            _thread = new Thread(RunEvents);            
        }

        public void Start()
        {
            _shutdown = false;
            _thread.Start();
        }

        public void Stop()
        {
            _shutdown = true;
            _thread.Join();
        }

        private void RunEvents()
        {            
            while (!_shutdown)
            {
                try
                {
                    var eventId = _queueProvider.DequeueWork(QueueType.Event).Result;
                    if (eventId != null)
                    {
                        if (_lockProvider.AcquireLock($"evt:{eventId}").Result)
                        {
                            try
                            {
                                var evt = _persistenceStore.GetEvent(eventId).Result;
                                var subs = _persistenceStore.GetSubcriptions(evt.EventName, evt.EventKey, evt.EventTime).Result;
                                var success = true;

                                foreach (var sub in subs)
                                    success = success && SeedSubscription(evt, sub);

                                if (success)
                                    _persistenceStore.MarkEventProcessed(eventId).Wait();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                            }
                            finally
                            {
                                _lockProvider.ReleaseLock($"evt:{eventId}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Event locked {eventId}");
                        }
                        
                    }
                    else
                    {
                        Thread.Sleep(_options.IdleTime); //no work
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        private bool SeedSubscription(Event evt, EventSubscription sub)
        {
            if (_lockProvider.AcquireLock(sub.WorkflowId).Result)
            {
                try
                {
                    var workflow = _persistenceStore.GetWorkflowInstance(sub.WorkflowId).Result;
                    var pointers = workflow.ExecutionPointers.Where(p => p.EventName == sub.EventName && p.EventKey == p.EventKey && !p.EventPublished);
                    foreach (var p in pointers)
                    {
                        p.EventData = evt.EventData;
                        p.EventPublished = true;
                        p.Active = true;
                    }
                    workflow.NextExecution = 0;
                    _persistenceStore.PersistWorkflow(workflow).Wait();
                    _persistenceStore.TerminateSubscription(sub.Id).Wait();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return false;
                }
                finally
                {
                    _lockProvider.ReleaseLock(sub.WorkflowId).Wait();
                    _queueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
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
