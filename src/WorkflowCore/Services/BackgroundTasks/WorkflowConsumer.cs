﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class WorkflowConsumer : QueueConsumer, IBackgroundTask
    {
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IWorkflowExecutor _executor;

        protected override int MaxConcurrentItems => Options.MaxConcurrentWorkflows;
        protected override QueueType Queue => QueueType.Workflow;

        public WorkflowConsumer(IPersistenceProvider persistenceProvider, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, IDateTimeProvider datetimeProvider, WorkflowOptions options)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStore = persistenceProvider;
            _executor = executor;
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            if (!await _lockProvider.AcquireLock(itemId, cancellationToken))
            {
                Logger.LogInformation("Workflow locked {0}", itemId);
                return;
            }
            
            WorkflowInstance workflow = null;
            WorkflowExecutorResult result = null;
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                workflow = await _persistenceStore.GetWorkflowInstance(itemId, cancellationToken);
                if (workflow.Status == WorkflowStatus.Runnable)
                {
                    try
                    {
                        result = await _executor.Execute(workflow, cancellationToken);
                    }
                    finally
                    {
                        await _persistenceStore.PersistWorkflow(workflow, cancellationToken);
                        await QueueProvider.QueueWork(itemId, QueueType.Index); // TODO: if workflow completed should not go in the queue again, right?!
                        await _lockProvider.ReleaseLock($"wf:{itemId}");
                    }
                }
            }
            finally
            {
                await _lockProvider.ReleaseLock(itemId);
                if ((workflow != null) && (result != null))
                {
                    foreach (var sub in result.Subscriptions)
                    {
                        await SubscribeEvent(sub, _persistenceStore, cancellationToken);
                    }

                    await _persistenceStore.PersistErrors(result.Errors, cancellationToken);

                    var readAheadTicks = _datetimeProvider.UtcNow.Add(Options.PollInterval).Ticks;

                    if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < readAheadTicks)
                    {
                        new Task(() => FutureQueue(workflow, cancellationToken)).Start();
                    }
                }
            }
            
        }
        
        private async Task SubscribeEvent(EventSubscription subscription, IPersistenceProvider persistenceStore, CancellationToken cancellationToken)
        {
            //TODO: move to own class
            Logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", subscription.EventName, subscription.EventKey, subscription.WorkflowId, subscription.StepId);
            
            await persistenceStore.CreateEventSubscription(subscription, cancellationToken);
            if (subscription.EventName != Event.EventTypeActivity)
            {
                var events = await persistenceStore.GetEvents(subscription.EventName, subscription.EventKey, subscription.SubscribeAsOf, cancellationToken);
                foreach (var evt in events)
                {
                    await persistenceStore.MarkEventUnprocessed(evt, cancellationToken);
                    await QueueProvider.QueueWork(evt, QueueType.Event);
                }
            }
        }

        private async void FutureQueue(WorkflowInstance workflow, CancellationToken cancellationToken)
        {
            try
            {
                if (!workflow.NextExecution.HasValue)
                {
                    return;
                }

                var target = (workflow.NextExecution.Value - _datetimeProvider.UtcNow.Ticks);
                if (target > 0)
                {
                    await Task.Delay(TimeSpan.FromTicks(target), cancellationToken);
                }

                await QueueProvider.QueueWork(workflow.Id, QueueType.Workflow);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }
    }
}
