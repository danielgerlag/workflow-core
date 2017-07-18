using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public abstract class QueueTaskDispatcher : IBackgroundTask
    {
        protected abstract QueueType Queue { get; }
        protected virtual int MaxConcurrentItems => Math.Max(Environment.ProcessorCount, 2);

        protected readonly IQueueProvider QueueProvider;
        protected readonly ILogger Logger;
        protected readonly WorkflowOptions Options;
        protected Task DispatchTask;
        private SemaphoreSlim _semaphore;
        private CancellationTokenSource _cancellationTokenSource;

        protected QueueTaskDispatcher(IQueueProvider queueProvider, ILoggerFactory loggerFactory, WorkflowOptions options)
        {
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        protected abstract Task ProcessItem(string itemId, CancellationToken cancellationToken);

        public virtual void Start()
        {
            if (DispatchTask != null)
                throw new InvalidOperationException();

            _cancellationTokenSource = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(MaxConcurrentItems);
            DispatchTask = new Task(Execute);
            DispatchTask.Start();
        }

        public virtual void Stop()
        {
            _cancellationTokenSource.Cancel();
            DispatchTask.Wait();

            for (var i = 0; i < MaxConcurrentItems; i++)
                _semaphore.Wait();

            DispatchTask = null;
        }

        private async void Execute()
        {
            var cancelToken = _cancellationTokenSource.Token;
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(cancelToken);
                    string item;
                    try
                    {
                        item = await QueueProvider.DequeueWork(Queue, cancelToken);
                    }
                    catch
                    {
                        _semaphore.Release();
                        throw;
                    }

                    if (item == null)
                    {
                        _semaphore.Release();
                        if (!QueueProvider.IsDequeueBlocking)
                            await Task.Delay(Options.IdleTime, cancelToken);
                        continue;
                    }

                    new Task(() => ExecuteItem(item, cancelToken)).Start();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }
        }

        private async void ExecuteItem(string itemId, CancellationToken cancellationToken)
        {
            try
            {
                await ProcessItem(itemId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation($"Operation cancelled while processing {itemId}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error executing item {itemId} - {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
