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
        private async void RunWorkflows()
        {
            while (!_shutdown)
            {
                try
                {
                    var workflowId = await _queueProvider.DequeueWork(QueueType.Workflow);
                    if (workflowId != null)
                    {
                        try
                        {
                            if (await _lockProvider.AcquireLock(workflowId))
                            {
                                WorkflowInstance workflow = null;
                                WorkflowExecutorResult result = null;
                                try
                                {
                                    workflow = await _persistenceStore.GetWorkflowInstance(workflowId);
                                    if (workflow.Status == WorkflowStatus.Runnable)
                                    {
                                        try
                                        {
                                            result = _executor.Execute(workflow, _options);
                                        }
                                        finally
                                        {
                                            await _persistenceStore.PersistWorkflow(workflow);
                                        }
                                    }
                                }
                                finally
                                {
                                    await _lockProvider.ReleaseLock(workflowId);
                                    if ((workflow != null) && (result != null))
                                    {
                                        foreach (var sub in result.Subscriptions)
                                            await SubscribeEvent(sub);

                                        await _persistenceStore.PersistErrors(result.Errors);

                                        if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < DateTime.Now.ToUniversalTime().Ticks)
                                            await _queueProvider.QueueWork(workflowId, QueueType.Workflow);
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
                        await Task.Delay(_options.IdleTime);  //no work
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        private async Task SubscribeEvent(EventSubscription subscription)
        {
            //TODO: move to own class
            _logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", subscription.EventName, subscription.EventKey, subscription.WorkflowId, subscription.StepId);
            
            await _persistenceStore.CreateEventSubscription(subscription);
            var events = await _persistenceStore.GetEvents(subscription.EventName, subscription.EventKey, subscription.SubscribeAsOf);
            foreach (var evt in events)
            {
                await _persistenceStore.MarkEventUnprocessed(evt);
                await _queueProvider.QueueWork(evt, QueueType.Event);
            }
        }
    }
}
