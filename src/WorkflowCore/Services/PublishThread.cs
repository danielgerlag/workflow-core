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
    class PublishThread : IPublishThread
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private bool _shutdown = true;
        private Thread _thread;

        public PublishThread(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<PublishThread>();
            _lockProvider = lockProvider;
            _thread = new Thread(RunPublications);            
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

        private void RunPublications()
        {            
            while (!_shutdown)
            {
                try
                {
                    var pub = _queueProvider.DequeueForPublishing().Result;
                    if (pub != null)
                    {
                        try
                        {
                            if (_lockProvider.AcquireLock(pub.WorkflowId).Result)
                            {
                                try
                                {
                                    var workflow = _persistenceStore.GetWorkflowInstance(pub.WorkflowId).Result;
                                    var pointers = workflow.ExecutionPointers.Where(p => p.EventName == pub.EventName && p.EventKey == p.EventKey && !p.EventPublished);
                                    foreach (var p in pointers)
                                    {
                                        p.EventData = pub.EventData;
                                        p.EventPublished = true;
                                        p.Active = true;
                                    }
                                    workflow.NextExecution = 0;
                                    _persistenceStore.PersistWorkflow(workflow);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex.Message);
                                    _persistenceStore.CreateUnpublishedEvent(pub); //retry later                                    
                                }
                                finally
                                {
                                    _lockProvider.ReleaseLock(pub.WorkflowId).Wait();
                                    _queueProvider.QueueForProcessing(pub.WorkflowId);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Workflow locked {0}", pub.WorkflowId);
                                _persistenceStore.CreateUnpublishedEvent(pub); //retry later
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
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
    }
}
