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
    class WorkflowTaskDispatcher : QueueTaskDispatcher, IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWorkflowExecutor _executor;
        private readonly IDateTimeProvider _datetimeProvider;

        protected override QueueType Queue => QueueType.Workflow;

        public WorkflowTaskDispatcher(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, IDateTimeProvider datetimeProvider, WorkflowOptions options)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStore = persistenceStore;
            _executor = executor;
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
            persistenceStore.EnsureStoreExists();
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            if (await _lockProvider.AcquireLock(itemId, cancellationToken))
            {
                WorkflowInstance workflow = null;
                WorkflowExecutorResult result = null;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    workflow = await _persistenceStore.GetWorkflowInstance(itemId);
                    if (workflow.Status == WorkflowStatus.Runnable)
                    {
                        try
                        {
                            result = await _executor.Execute(workflow, Options);
                        }
                        finally
                        {
                            await _persistenceStore.PersistWorkflow(workflow);
                        }
                    }
                }
                finally
                {
                    await _lockProvider.ReleaseLock(itemId);
                    if ((workflow != null) && (result != null))
                    {
                        foreach (var sub in result.Subscriptions)
                            await SubscribeEvent(sub);

                        await _persistenceStore.PersistErrors(result.Errors);

                        var readAheadTicks = _datetimeProvider.Now.Add(Options.PollInterval).ToUniversalTime().Ticks;

                        if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < readAheadTicks)
                        {
                            new Task(() => FutureQueue(workflow, cancellationToken)).Start();
                        }
                    }
                }
            }
            else
            {
                Logger.LogInformation("Workflow locked {0}", itemId);
            }
        }
        
        private async Task SubscribeEvent(EventSubscription subscription)
        {
            //TODO: move to own class
            Logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", subscription.EventName, subscription.EventKey, subscription.WorkflowId, subscription.StepId);
            
            await _persistenceStore.CreateEventSubscription(subscription);
            var events = await _persistenceStore.GetEvents(subscription.EventName, subscription.EventKey, subscription.SubscribeAsOf);
            foreach (var evt in events)
            {
                await _persistenceStore.MarkEventUnprocessed(evt);
                await QueueProvider.QueueWork(evt, QueueType.Event);
            }
        }

        private async void FutureQueue(WorkflowInstance workflow, CancellationToken cancellationToken)
        {
            try
            {
                if (!workflow.NextExecution.HasValue)
                    return;

                var target = (workflow.NextExecution.Value - _datetimeProvider.Now.ToUniversalTime().Ticks);
                if (target > 0)
                    await Task.Delay(TimeSpan.FromTicks(target), cancellationToken);

                await QueueProvider.QueueWork(workflow.Id, QueueType.Workflow);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }
    }
}
