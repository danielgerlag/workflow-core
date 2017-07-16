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
    class WorkflowTask : IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWorkflowExecutor _executor;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly IList<Task> _tasks;
        private readonly WorkflowOptions _options;
        private readonly IDateTimeProvider _datetimeProvider;
        private bool _shutdown = true;

        public WorkflowTask(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, IDateTimeProvider datetimeProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _executor = executor;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowTask>();
            _lockProvider = lockProvider;

            _tasks = new List<Task>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
                _tasks.Add(new Task(RunWorkflows));

            _datetimeProvider = datetimeProvider;
            persistenceStore.EnsureStoreExists();
        }

        public void Start()
        {
            _shutdown = false;
            foreach (var task in _tasks)
                task.Start();
        }

        public void Stop()
        {
            _shutdown = true;
            foreach (var task in _tasks)
                task.Wait();
        }

        /// <summary>
        /// Worker task body
        /// </summary>        
        private async void RunWorkflows()
        {
            while (!_shutdown)
            {
                try
                {
                    var workflowId = await _queueProvider.DequeueWork(QueueType.Workflow);
                    
                    if (workflowId != null)
                        await ProcessWorkflow(workflowId);
                    else
                        await Task.Delay(_options.IdleTime);  //no work
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        private async Task ProcessWorkflow(string workflowId)
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
                                result = await _executor.Execute(workflow, _options);
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

                            var readAheadTicks = _datetimeProvider.Now.Add(_options.PollInterval).ToUniversalTime().Ticks;

                            if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < readAheadTicks)
                            {
                                Parallel.Invoke(async () =>
                                {
                                    if (!workflow.NextExecution.HasValue)
                                        return;

                                    var target = (workflow.NextExecution.Value - _datetimeProvider.Now.ToUniversalTime().Ticks);
                                    if (target > 0)
                                        await Task.Delay(TimeSpan.FromTicks(target));

                                    await _queueProvider.QueueWork(workflowId, QueueType.Workflow);
                                });
                            }
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
