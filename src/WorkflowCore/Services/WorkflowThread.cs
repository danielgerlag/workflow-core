using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    class WorkflowThread : IWorkflowThread
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWorkflowExecutor _executor;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly WorkflowOptions _options;
        private bool _shutdown = true;
        private Thread _thread;

        public WorkflowThread(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _executor = executor;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowThread>();
            _lockProvider = lockProvider;
            _thread = new Thread(RunWorkflows);
            persistenceStore.EnsureStoreExists();
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

        /// <summary>
        /// Worker thread body
        /// </summary>        
        private void RunWorkflows()
        {
            while (!_shutdown)
            {
                try
                {
                    var workflowId = _queueProvider.DequeueWork(QueueType.Workflow).Result;
                    if (workflowId != null)
                    {
                        try
                        {
                            if (_lockProvider.AcquireLock(workflowId).Result)
                            {
                                WorkflowInstance workflow = null;
                                WorkflowExecutorResult result = null;
                                try
                                {
                                    workflow = _persistenceStore.GetWorkflowInstance(workflowId).Result;
                                    if (workflow.Status == WorkflowStatus.Runnable)
                                    {
                                        try
                                        {
                                            result = _executor.Execute(workflow, _options);
                                        }
                                        finally
                                        {
                                            _persistenceStore.PersistWorkflow(workflow).Wait();
                                        }
                                    }
                                }
                                finally
                                {
                                    _lockProvider.ReleaseLock(workflowId).Wait();
                                    if ((workflow != null) && (result != null))
                                    {
                                        foreach (var sub in result.Subscriptions)
                                            SubscribeEvent(sub);

                                        _persistenceStore.PersistErrors(result.Errors);

                                        if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < DateTime.Now.ToUniversalTime().Ticks)
                                            _queueProvider.QueueWork(workflowId, QueueType.Workflow);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Workflow locked {0}", workflowId);
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

        private void SubscribeEvent(EventSubscription subscription)
        {
            //TODO: move to own class
            _logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", subscription.EventName, subscription.EventKey, subscription.WorkflowId, subscription.StepId);
            
            _persistenceStore.CreateEventSubscription(subscription).Wait();
            var events = _persistenceStore.GetEvents(subscription.EventName, subscription.EventKey, subscription.SubscribeAsOf).Result;
            foreach (var evt in events)
            {
                _persistenceStore.MarkEventUnprocessed(evt).Wait();
                _queueProvider.QueueWork(evt, QueueType.Event);
            }
        }
    }
}
