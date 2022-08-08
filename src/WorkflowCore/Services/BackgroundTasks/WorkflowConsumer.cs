﻿using System;
using System.Diagnostics;
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
        private readonly IGreyList _greylist;

        protected override int MaxConcurrentItems => Options.MaxConcurrentWorkflows;
        protected override QueueType Queue => QueueType.Workflow;

        public WorkflowConsumer(IPersistenceProvider persistenceProvider, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IWorkflowExecutor executor, IDateTimeProvider datetimeProvider, IGreyList greylist, WorkflowOptions options)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStore = persistenceProvider;
            _greylist = greylist;
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

                WorkflowActivity.Enrich(workflow, "process");
                if (workflow.Status == WorkflowStatus.Runnable)
                {
                    try
                    {
                        result = await _executor.Execute(workflow, cancellationToken);
                    }
                    finally
                    {
                        WorkflowActivity.Enrich(result);
                        await _persistenceStore.PersistWorkflow(workflow);
                        await QueueProvider.QueueWork(itemId, QueueType.Index);
                        _greylist.Remove($"wf:{itemId}");
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

                    await _persistenceStore.PersistErrors(result.Errors);

                    if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue)
                    {
                        var readAheadTicks = _datetimeProvider.UtcNow.Add(Options.PollInterval).Ticks;
                        if (workflow.NextExecution.Value < readAheadTicks)
                        {
                            new Task(() => FutureQueue(workflow, cancellationToken)).Start();
                        }
                        else
                        {
                            if (_persistenceStore.SupportsScheduledCommands)
                            {
                                await _persistenceStore.ScheduleCommand(new ScheduledCommand()
                                {
                                    CommandName = ScheduledCommand.ProcessWorkflow,
                                    Data = workflow.Id,
                                    ExecuteTime = workflow.NextExecution.Value
                                });
                            }
                        }
                    }
                }
            }

        }

        private async Task SubscribeEvent(EventSubscription subscription, IPersistenceProvider persistenceStore, CancellationToken cancellationToken)
        {
            //TODO: move to own class
            Logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", subscription.EventName, subscription.EventKey, subscription.WorkflowId, subscription.StepId);

            await persistenceStore.CreateEventSubscription(subscription);
            if (subscription.EventName != Event.EventTypeActivity)
            {
                var events = await persistenceStore.GetEvents(subscription.EventName, subscription.EventKey, subscription.SubscribeAsOf, cancellationToken);

                foreach (var evt in events)
                {
                    var eventKey = $"evt:{evt}";
                    bool acquiredLock = false;
                    try
                    {
                        acquiredLock = await _lockProvider.AcquireLock(eventKey, cancellationToken);
                        int attempt = 0;
                        while (!acquiredLock && attempt < 10)
                        {
                            await Task.Delay(Options.IdleTime, cancellationToken);
                            acquiredLock = await _lockProvider.AcquireLock(eventKey, cancellationToken);

                            attempt++;
                        }

                        if (!acquiredLock)
                        {
                            Logger.LogWarning($"Failed to lock {evt}");
                        }
                        else
                        {
                            _greylist.Remove(eventKey);
                            await persistenceStore.MarkEventUnprocessed(evt);
                            await QueueProvider.QueueWork(evt, QueueType.Event);
                        }
                    }
                    finally
                    {
                        if (acquiredLock)
                        {
                            await _lockProvider.ReleaseLock(eventKey);
                        }
                    }
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
