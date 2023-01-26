using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks.RunnablePoller
{
    internal class WorkflowRunnablePoller : BaseWorkflowRunnablePoller
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly IGreyList _greylist;
        private readonly IDateTimeProvider _dateTimeProvider;

        public WorkflowRunnablePoller(
            IPersistenceProvider persistenceStore,
            IQueueProvider queueProvider,
            ILoggerFactory loggerFactory,
            IDistributedLockProvider lockProvider,
            IGreyList greylist,
            IDateTimeProvider dateTimeProvider,
            WorkflowOptions options)
            : base(options.PollWorkflowsInterval)
        {
            _persistenceStore = persistenceStore;
            _greylist = greylist;
            _queueProvider = queueProvider;
            _logger = loggerFactory.CreateLogger<WorkflowRunnablePoller>();
            _lockProvider = lockProvider;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override async void PollRunnables(object target)
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
                                    activity?.RecordException(ex);
                                }
                            }
                            if (_greylist.Contains($"wf:{item}"))
                            {
                                _logger.LogDebug($"Got greylisted workflow {item}");
                                continue;
                            }
                            _logger.LogDebug("Got runnable instance {0}", item);
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
                activity?.RecordException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }

    }
}
