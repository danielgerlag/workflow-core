using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class EventConsumer : QueueConsumer, IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _datetimeProvider;

        protected override QueueType Queue => QueueType.Event;

        public EventConsumer(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options, IDateTimeProvider datetimeProvider)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStore = persistenceStore;
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            if (await _lockProvider.AcquireLock($"evt:{itemId}", cancellationToken))
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var evt = await _persistenceStore.GetEvent(itemId);
                    if (evt.EventTime <= _datetimeProvider.Now.ToUniversalTime())
                    {
                        var subs = await _persistenceStore.GetSubcriptions(evt.EventName, evt.EventKey, evt.EventTime);
                        var success = true;

                        foreach (var sub in subs.ToList())
                        {
                            success = success && await SeedSubscription(evt, sub, cancellationToken);
                        }

                        if (success)
                        {
                            await _persistenceStore.MarkEventProcessed(itemId);
                        }
                    }
                }
                finally
                {
                    await _lockProvider.ReleaseLock($"evt:{itemId}");
                }
            }
            else
            {
                Logger.LogInformation($"Event locked {itemId}");
            }
        }
        
        private async Task<bool> SeedSubscription(Event evt, EventSubscription sub, CancellationToken cancellationToken)
        {
            if (await _lockProvider.AcquireLock(sub.WorkflowId, cancellationToken))
            {
                try
                {
                    var workflow = await _persistenceStore.GetWorkflowInstance(sub.WorkflowId);
                    var pointers = workflow.ExecutionPointers.Where(p => p.EventName == sub.EventName && p.EventKey == sub.EventKey && !p.EventPublished && p.EndTime == null);
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
                    Logger.LogError(ex.Message);
                    return false;
                }
                finally
                {
                    await _lockProvider.ReleaseLock(sub.WorkflowId);
                    await QueueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
                }
            }
            else
            {
                Logger.LogInformation("Workflow locked {0}", sub.WorkflowId);
                return false;
            }
        }
    }
}
