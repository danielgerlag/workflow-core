using System;
using System.Collections.Generic;
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
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly IGreyList _greylist;

        protected override QueueType Queue => QueueType.Event;

        public EventConsumer(IWorkflowRepository workflowRepository, ISubscriptionRepository subscriptionRepository, IEventRepository eventRepository, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, WorkflowOptions options, IDateTimeProvider datetimeProvider, IGreyList greylist)
            : base(queueProvider, loggerFactory, options)
        {
            _workflowRepository = workflowRepository;
            _greylist = greylist;
            _subscriptionRepository = subscriptionRepository;
            _eventRepository = eventRepository;
            _lockProvider = lockProvider;
            _datetimeProvider = datetimeProvider;
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            if (!await _lockProvider.AcquireLock($"evt:{itemId}", cancellationToken))
            {
                Logger.LogInformation($"Event locked {itemId}");
                return;
            }
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var evt = await _eventRepository.GetEvent(itemId, cancellationToken);

                WorkflowActivity.Enrich(evt);
                if (evt.IsProcessed)
                {
                    _greylist.Add($"evt:{evt.Id}");
                    return;
                }
                if (evt.EventTime <= _datetimeProvider.UtcNow)
                {
                    IEnumerable<EventSubscription> subs = null;
                    if (evt.EventData is ActivityResult)
                    {
                        var activity = await _subscriptionRepository.GetSubscription((evt.EventData as ActivityResult).SubscriptionId, cancellationToken);
                        if (activity == null)
                        {
                            Logger.LogWarning($"Activity already processed - {(evt.EventData as ActivityResult).SubscriptionId}");
                            await _eventRepository.MarkEventProcessed(itemId, cancellationToken);
                            return;
                        }
                        subs = new List<EventSubscription> { activity };
                    }
                    else
                    {
                        subs = await _subscriptionRepository.GetSubscriptions(evt.EventName, evt.EventKey, evt.EventTime, cancellationToken);
                    }

                    var toQueue = new HashSet<string>();
                    var complete = true;

                    foreach (var sub in subs.ToList())
                        complete = complete && await SeedSubscription(evt, sub, toQueue, cancellationToken);

                    if (complete)
                    {
                        await _eventRepository.MarkEventProcessed(itemId, cancellationToken);
                    }
                    else
                    {
                        _greylist.Remove($"evt:{evt.Id}");
                    }

                    foreach (var eventId in toQueue)
                        await QueueProvider.QueueWork(eventId, QueueType.Event);
                }
            }
            finally
            {
                await _lockProvider.ReleaseLock($"evt:{itemId}");
            }
        }
        
        private async Task<bool> SeedSubscription(Event evt, EventSubscription sub, HashSet<string> toQueue, CancellationToken cancellationToken)
        {
            foreach (var eventId in await _eventRepository.GetEvents(sub.EventName, sub.EventKey, sub.SubscribeAsOf, cancellationToken))
            {
                if (eventId == evt.Id)
                    continue;

                var siblingEvent = await _eventRepository.GetEvent(eventId, cancellationToken);
                if ((!siblingEvent.IsProcessed) && (siblingEvent.EventTime < evt.EventTime))
                {
                    await QueueProvider.QueueWork(eventId, QueueType.Event);
                    return false;
                }

                if (!siblingEvent.IsProcessed)
                    toQueue.Add(siblingEvent.Id);
            }

            if (!await _lockProvider.AcquireLock(sub.WorkflowId, cancellationToken))
            {
                Logger.LogInformation("Workflow locked {WorkflowId}", sub.WorkflowId);
                return false;
            }
            
            try
            {
                var workflow = await _workflowRepository.GetWorkflowInstance(sub.WorkflowId, cancellationToken);
                IEnumerable<ExecutionPointer> pointers = null;
                
                if (!string.IsNullOrEmpty(sub.ExecutionPointerId))
                    pointers = workflow.ExecutionPointers.Where(p => p.Id == sub.ExecutionPointerId && !p.EventPublished && p.EndTime == null);
                else
                    pointers = workflow.ExecutionPointers.Where(p => p.EventName == sub.EventName && p.EventKey == sub.EventKey && !p.EventPublished && p.EndTime == null);

                foreach (var p in pointers)
                {
                    p.EventData = evt.EventData;
                    p.EventPublished = true;
                    p.Active = true;
                }
                workflow.NextExecution = 0;
                await _workflowRepository.PersistWorkflow(workflow, cancellationToken);
                await _subscriptionRepository.TerminateSubscription(sub.Id, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return false;
            }
            finally
            {
                await _lockProvider.ReleaseLock(sub.WorkflowId);
                await QueueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
            }
        }
    }
}