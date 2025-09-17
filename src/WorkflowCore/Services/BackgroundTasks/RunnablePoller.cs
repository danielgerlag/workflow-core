using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class RunnablePoller : IBackgroundTask
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly IGreyList _greylist;
        private readonly WorkflowOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;
        private Timer _pollTimer;

        public RunnablePoller(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IGreyList greylist, IDateTimeProvider dateTimeProvider, WorkflowOptions options)
        {
            _persistenceStore = persistenceStore;
            _greylist = greylist;
            _queueProvider = queueProvider;            
            _logger = loggerFactory.CreateLogger<RunnablePoller>();
            _lockProvider = lockProvider;
            _dateTimeProvider = dateTimeProvider;
            _options = options;
        }

        public void Start()
        {
            _pollTimer = new Timer(new TimerCallback(PollRunnables), null, TimeSpan.FromSeconds(0), _options.PollInterval);
        }

        public void Stop()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        /// <summary>
        /// Poll the persistence store for workflows ready to run.
        /// Poll the persistence store for stashed unpublished events
        /// </summary>        
        private async void PollRunnables(object target)
        {
            await PollWorkflows();
            await PollEvents();
            await PollCommands();
        }

        private async Task PollWorkflows()
        {
            var activity = WorkflowActivity.StartPoll("workflows");
            try
            {
                if (await _lockProvider.AcquireLock("poll runnables", new CancellationToken()))
                {
                    try
                    {
                        _logger.LogDebug("Polling for runnable workflows");

                        var runnables = await _persistenceStore.GetRunnableInstances(_dateTimeProvider.Now);
                        foreach (var item in runnables)
                        {
                            if (_persistenceStore.SupportsScheduledCommands)
                            {
                                try
                                {
                                    await _persistenceStore.ScheduleCommand(new ScheduledCommand()
                                    {
                                        CommandName = ScheduledCommand.ProcessWorkflow,
                                        Data = item,
                                        ExecuteTime = _dateTimeProvider.UtcNow.Ticks
                                    });
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, ex.Message);
                                    activity?.AddException(ex);
                                }
                            }
                            if (_greylist.Contains($"wf:{item}"))
                            {
                                _logger.LogDebug($"Got greylisted workflow {item}");
                                continue;
                            }
                            _logger.LogDebug("Got runnable instance {Item}", item);
                            _greylist.Add($"wf:{item}");
                            await _queueProvider.QueueWork(item, QueueType.Workflow);
                        }
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock("poll runnables");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                activity?.AddException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private async Task PollEvents()
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
                                    activity?.AddException(ex);
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
                activity?.AddException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private async Task PollCommands()
        {
            var activity = WorkflowActivity.StartPoll("commands");
            try
            {
                if (!_persistenceStore.SupportsScheduledCommands)
                    return;

                if (await _lockProvider.AcquireLock("poll-commands", new CancellationToken()))
                {
                    try
                    {
                        _logger.LogDebug("Polling for scheduled commands");
                        await _persistenceStore.ProcessCommands(new DateTimeOffset(_dateTimeProvider.UtcNow), async (command) =>
                        {
                            switch (command.CommandName)
                            {
                                case ScheduledCommand.ProcessWorkflow:
                                    await _queueProvider.QueueWork(command.Data, QueueType.Workflow);
                                    break;
                                case ScheduledCommand.ProcessEvent:
                                    await _queueProvider.QueueWork(command.Data, QueueType.Event);
                                    break;
                            }
                        });
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock("poll-commands");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                activity?.AddException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }
}