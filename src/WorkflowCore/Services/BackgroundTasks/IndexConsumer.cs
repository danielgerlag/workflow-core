using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services.BackgroundTasks
{
    internal class IndexConsumer : QueueConsumer, IBackgroundTask
    {
        private readonly ISearchIndex _searchIndex;
        private readonly ObjectPool<IPersistenceProvider> _persistenceStorePool;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, int> _errorCounts = new ConcurrentDictionary<string, int>();

        protected override QueueType Queue => QueueType.Index;
        protected override bool EnableSecondPasses => true;

        public IndexConsumer(IPooledObjectPolicy<IPersistenceProvider> persistencePoolPolicy, IQueueProvider queueProvider, ILoggerFactory loggerFactory, ISearchIndex searchIndex, WorkflowOptions options)
            : base(queueProvider, loggerFactory, options)
        {
            _persistenceStorePool = new DefaultObjectPool<IPersistenceProvider>(persistencePoolPolicy);
            _searchIndex = searchIndex;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
        {
            try
            {
                var workflow = await FetchWorkflow(itemId);
                
                WorkflowActivity.Enrich(workflow, "index");
                await _searchIndex.IndexWorkflow(workflow);
                _errorCounts.TryRemove(itemId, out _);
            }
            catch (Exception e)
            {
                Logger.LogWarning(default(EventId), $"Error indexing workfow - {itemId} - {e.Message}");
                var errCount = _errorCounts.AddOrUpdate(itemId, 1, (key, count) => count + 1);
                
                if (errCount < 5)
                {
                    await QueueProvider.QueueWork(itemId, Queue);
                    return;
                }
                if (errCount < 20)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await QueueProvider.QueueWork(itemId, Queue);
                    return;
                }

                _errorCounts.TryRemove(itemId, out _);

                Logger.LogError(default(EventId), e, $"Unable to index workfow - {itemId} - {e.Message}");
            }
        }

        private async Task<WorkflowInstance> FetchWorkflow(string id)
        {
            var store = _persistenceStorePool.Get();
            try
            {
                return await store.GetWorkflowInstance(id);
            }
            finally
            {
                _persistenceStorePool.Return(store);
            }
        }
        
    }
}
