using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    /// <summary>
    /// Background task responsible for consuming workflow items from the queue and processing them.
    /// This consumer ensures that workflows are removed from the greylist after processing,
    /// regardless of their status, to prevent workflows from getting stuck in "Pending" state.
    /// </summary>
    internal class WorkflowConsumer : QueueConsumer, IBackgroundTask
    {
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IWorkflowExecutor _executor;
        private readonly IGreyList _greylist;
        private readonly ConcurrentDictionary<string, int> _loadFailures = new ConcurrentDictionary<string, int>();

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
            if (IsQuarantined(itemId))
            {
                Logger.LogDebug("Skipping quarantined workflow {ItemId}", itemId);
                _greylist.Add($"wf:{itemId}");
                return;
            }

            if (!await _lockProvider.AcquireLock(itemId, cancellationToken))
            {
                Logger.LogInformation("Workflow locked {ItemId}", itemId);
                return;
            }

            WorkflowInstance workflow = null;
            WorkflowExecutorResult result = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                workflow = await LoadWorkflow(itemId, cancellationToken);

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
                        await _persistenceStore.PersistWorkflow(workflow, result?.Subscriptions, cancellationToken);
                        await QueueProvider.QueueWork(itemId, QueueType.Index);
                    }
                }
                else
                {
                    Logger.LogDebug("Workflow {ItemId} is not runnable, status: {Status}", itemId, workflow.Status);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing workflow {ItemId}", itemId);
                throw;
            }
            finally
            {
                // Always remove from greylist regardless of workflow status
                // This prevents workflows from being stuck in greylist when they can't be processed
                if (IsQuarantined(itemId))
                {
                    // Keep it greylisted so the poller stops re-queuing it
                    _greylist.Add($"wf:{itemId}");
                }
                else
                {
                    Logger.LogDebug("Removing workflow {ItemId} from greylist", itemId);
                    _greylist.Remove($"wf:{itemId}");
                }
                
                await _lockProvider.ReleaseLock(itemId);
                if ((workflow != null) && (result != null))
                {
                    foreach (var sub in result.Subscriptions)
                    {
                        await TryProcessSubscription(sub, _persistenceStore, cancellationToken);
                    }

                    await _persistenceStore.PersistErrors(result.Errors, cancellationToken);

                    if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue)
                    {
                        var readAheadTicks = _datetimeProvider.UtcNow.Add(Options.PollInterval).Ticks;
                        if (workflow.NextExecution.Value < readAheadTicks)
                        {
                            _ = FutureQueue(workflow, cancellationToken);
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

        private bool IsQuarantined(string itemId)
        {
            return _loadFailures.TryGetValue(itemId, out var failures) && failures >= Options.MaxWorkflowLoadAttempts;
        }

        private async Task<WorkflowInstance> LoadWorkflow(string itemId, CancellationToken cancellationToken)
        {
            try
            {
                var workflow = await _persistenceStore.GetWorkflowInstance(itemId, cancellationToken);
                _loadFailures.TryRemove(itemId, out _);
                return workflow;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                var failures = _loadFailures.AddOrUpdate(itemId, 1, (key, count) => count + 1);
                if (failures >= Options.MaxWorkflowLoadAttempts)
                {
                    Logger.LogError(ex, "Workflow {ItemId} failed to load {Failures} times in a row and is quarantined on this node until restart. Its stored state is unchanged.", itemId, failures);
                }
                throw;
            }
        }

        private async Task TryProcessSubscription(EventSubscription subscription, IPersistenceProvider persistenceStore, CancellationToken cancellationToken)
        {
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
                            await persistenceStore.MarkEventUnprocessed(evt, cancellationToken);
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

        private async Task FutureQueue(WorkflowInstance workflow, CancellationToken cancellationToken)
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