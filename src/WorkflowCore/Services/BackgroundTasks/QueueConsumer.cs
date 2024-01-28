using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    internal abstract class QueueConsumer : IBackgroundTask
    {
        private readonly Dictionary<string, EventWaitHandle> _activeTasks;
        // Refer to  https://github.com/dotnet/runtime/issues/39919#issuecomment-954774092
        private readonly ConcurrentDictionary<string, byte> _secondPasses;
        private CancellationTokenSource _cancellationTokenSource;

        protected readonly IQueueProvider QueueProvider;
        protected readonly ILogger Logger;
        protected readonly WorkflowOptions Options;
        protected Task DispatchTask;

        protected abstract QueueType Queue { get; }
        protected virtual int MaxConcurrentItems => Math.Max(Environment.ProcessorCount, 2);
        protected virtual bool EnableSecondPasses => false;

        protected QueueConsumer(IQueueProvider queueProvider, ILoggerFactory loggerFactory, WorkflowOptions options)
        {
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger(GetType());

            _activeTasks = new Dictionary<string, EventWaitHandle>();
            _secondPasses = new ConcurrentDictionary<string, byte>();
        }

        protected abstract Task ProcessItem(string itemId, CancellationToken cancellationToken);

        public virtual void Start()
        {
            if (DispatchTask != null)
            {
                throw new InvalidOperationException();
            }

            _cancellationTokenSource = new CancellationTokenSource();

            DispatchTask = Task.Factory.StartNew(Execute, TaskCreationOptions.LongRunning);
        }

        public virtual void Stop()
        {
            _cancellationTokenSource.Cancel();
            if (DispatchTask != null)
            {
                DispatchTask.Wait();
                DispatchTask = null;
            }
        }

        private async Task Execute()
        {
            var cancelToken = _cancellationTokenSource.Token;

            while (!cancelToken.IsCancellationRequested)
            {
                Activity activity = default;
                try
                {
                    var activeCount = 0;
                    lock (_activeTasks)
                    {
                        activeCount = _activeTasks.Count;
                    }
                    if (activeCount >= MaxConcurrentItems)
                    {
                        await Task.Delay(Options.IdleTime);
                        continue;
                    }

                    activity = WorkflowActivity.StartConsume(Queue);
                    var item = await QueueProvider.DequeueWork(Queue, cancelToken);

                    if (item == null)
                    {
                        activity?.Dispose();
                        if (!QueueProvider.IsDequeueBlocking)
                            await Task.Delay(Options.IdleTime, cancelToken);
                        continue;
                    }

                    activity?.EnrichWithDequeuedItem(item);

                    var hasTask = false;
                    lock (_activeTasks)
                    {
                        hasTask = _activeTasks.ContainsKey(item);
                    }
                    if (hasTask)
                    {
                        _secondPasses.TryAdd(item, 0);
                        if (!EnableSecondPasses)
                            await QueueProvider.QueueWork(item, Queue);
                        activity?.Dispose();
                        continue;
                    }

                    _secondPasses.TryRemove(item, out _);

                    var waitHandle = new ManualResetEvent(false);
                    lock (_activeTasks)
                    {
                        _activeTasks.Add(item, waitHandle);
                    }
                    var task = ExecuteItem(item, waitHandle, activity);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    activity?.RecordException(ex);
                }
                finally
                {
                    activity?.Dispose();
                }
            }

            List<EventWaitHandle> toComplete;
            lock (_activeTasks)
            {
                toComplete = _activeTasks.Values.ToList();
            }

            foreach (var handle in toComplete)
                handle.WaitOne();
        }

        private async Task ExecuteItem(string itemId, EventWaitHandle waitHandle, Activity activity)
        {
            try
            {
                await ProcessItem(itemId, _cancellationTokenSource.Token);
                while (EnableSecondPasses && _secondPasses.ContainsKey(itemId))
                {
                    _secondPasses.TryRemove(itemId, out _);
                    await ProcessItem(itemId, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation($"Operation cancelled while processing {itemId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(default(EventId), ex, $"Error executing item {itemId} - {ex.Message}");
                activity?.RecordException(ex);
            }
            finally
            {
                waitHandle.Set();
                lock (_activeTasks)
                {
                    _activeTasks.Remove(itemId);
                }
            }
        }
    }
}
