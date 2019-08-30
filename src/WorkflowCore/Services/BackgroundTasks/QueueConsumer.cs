using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal abstract class QueueConsumer : IBackgroundTask
    {
        protected abstract QueueType Queue { get; }
        protected virtual int MaxConcurrentItems => Math.Max(Environment.ProcessorCount, 2);
        protected virtual bool EnableSecondPasses => false;

        protected readonly IQueueProvider QueueProvider;
        protected readonly ILogger Logger;
        protected readonly WorkflowOptions Options;
        protected Task DispatchTask;        
        private CancellationTokenSource _cancellationTokenSource;

        protected QueueConsumer(IQueueProvider queueProvider, ILoggerFactory loggerFactory, WorkflowOptions options)
        {
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        protected abstract Task ProcessItem(string itemId, CancellationToken cancellationToken);

        public virtual void Start()
        {
            if (DispatchTask != null)
            {
                throw new InvalidOperationException();
            }

            _cancellationTokenSource = new CancellationTokenSource();
                        
            DispatchTask = new Task(Execute, TaskCreationOptions.LongRunning);
            DispatchTask.Start();
        }

        public virtual void Stop()
        {
            _cancellationTokenSource.Cancel();
            DispatchTask.Wait();
            DispatchTask = null;
        }

        private async void Execute()
        {
            var cancelToken = _cancellationTokenSource.Token;
            var activeTasks = new Dictionary<string, Task>();
            var secondPasses = new HashSet<string>();

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    if (activeTasks.Count >= MaxConcurrentItems)
                    {
                        await Task.Delay(Options.IdleTime);
                        continue;
                    }

                    var item = await QueueProvider.DequeueWork(Queue, cancelToken);

                    if (item == null)
                    {
                        if (!QueueProvider.IsDequeueBlocking)
                            await Task.Delay(Options.IdleTime, cancelToken);
                        continue;
                    }
                    
                    if (activeTasks.ContainsKey(item))
                    {
                        secondPasses.Add(item);
                        if (!EnableSecondPasses)
                            await QueueProvider.QueueWork(item, Queue);
                        continue;
                    }

                    secondPasses.Remove(item);

                    var task = new Task(async (object data) =>
                    {
                        try
                        {
                            await ExecuteItem((string)data);
                            while (EnableSecondPasses && secondPasses.Contains(item))
                            {
                                secondPasses.Remove(item);
                                await ExecuteItem((string)data);
                            }
                        }
                        finally
                        {
                            lock (activeTasks)
                            {
                                activeTasks.Remove((string)data);
                            }
                        }
                    }, item);
                    lock (activeTasks)
                    {
                        activeTasks.Add(item, task);
                    }
                    
                    task.Start();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }

            await Task.WhenAll(activeTasks.Values);
        }

        private async Task ExecuteItem(string itemId)
        {
            try
            {
                await ProcessItem(itemId, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation($"Operation cancelled while processing {itemId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(default(EventId), ex, $"Error executing item {itemId} - {ex.Message}");
            }
        }
    }
}
