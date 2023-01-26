using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks.RunnablePoller
{
    internal class CommandRunnablePoller : BaseWorkflowRunnablePoller
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IQueueProvider _queueProvider;
        private readonly ILogger _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CommandRunnablePoller(
            IPersistenceProvider persistenceStore,
            IQueueProvider queueProvider,
            ILoggerFactory loggerFactory,
            IDistributedLockProvider lockProvider,
            IDateTimeProvider dateTimeProvider,
            WorkflowOptions options)
            : base(options.PollCommandsInterval)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _logger = loggerFactory.CreateLogger<CommandRunnablePoller>();
            _lockProvider = lockProvider;
            _dateTimeProvider = dateTimeProvider;
        } 

        protected override async void PollRunnables(object target)
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
                activity?.RecordException(ex);
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }
}
