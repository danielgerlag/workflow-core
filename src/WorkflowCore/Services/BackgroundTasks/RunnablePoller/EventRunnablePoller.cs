using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks.RunnablePoller
{
    internal class EventRunnablePoller : BaseWorkflowRunnablePoller
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly IGreyList _greylist;
        private readonly IDateTimeProvider _dateTimeProvider;

        public EventRunnablePoller(
            IPersistenceProvider persistenceStore,
            IQueueProvider queueProvider,
            ILoggerFactory loggerFactory,
            IDistributedLockProvider lockProvider,
            IGreyList greylist,
            IDateTimeProvider dateTimeProvider,
            WorkflowOptions options)
            : base(options.PollEventsInterval)
        {
            _persistenceStore = persistenceStore;
            _greylist = greylist;
            _queueProvider = queueProvider;
            _logger = loggerFactory.CreateLogger<EventRunnablePoller>();
            _lockProvider = lockProvider;
            _dateTimeProvider = dateTimeProvider;
        }
     
        protected override async void PollRunnables(object target)
        {
            var activity = WorkflowActivity.StartPoll("events");
            try
            {
                if (await _lockProvider.AcquireLock("unprocessed events", new CancellationToken()))
                {
                    try
                    {
                        _logger.LogDebug("Polling for unprocessed events");

                        var events = await _persistenceStore.GetRunnableEvents(_dateTimeProvider.Now);
                        foreach (var item in events.ToList())
                        {
                            if (_persistenceStore.SupportsScheduledCommands)
                            {
                                try
                                {
                                    await _persistenceStore.ScheduleCommand(new ScheduledCommand()
                                    {
                                        CommandName = ScheduledCommand.ProcessEvent,
                                        Data = item,
                                        ExecuteTime = _dateTimeProvider.UtcNow.Ticks
                                    });
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, ex.Message);
                                    activity?.RecordException(ex);
                                }
                            }
                            if (_greylist.Contains($"evt:{item}"))
                            {
                                _logger.LogDebug($"Got greylisted event {item}");
                                continue;
                            }
                            _logger.LogDebug($"Got unprocessed event {item}");
                            _greylist.Add($"evt:{item}");
                            await _queueProvider.QueueWork(item, QueueType.Event);
                        }
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock("unprocessed events");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                activity?.RecordException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

    }
}
