using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Models
{
    public class WorkflowOptions
    {
        internal Func<IServiceProvider, IPersistenceProvider> PersistanceFactory;
        internal Func<IServiceProvider, IQueueProvider> QueueFactory;
        internal Func<IServiceProvider, IDistributedLockProvider> LockFactory;
        internal Func<IServiceProvider, ILifeCycleEventHub> EventHubFactory;
        internal Func<IServiceProvider, ISearchIndex> SearchIndexFactory;
        internal TimeSpan PollInterval;
        internal TimeSpan IdleTime;
        internal TimeSpan ErrorRetryInterval;
        internal int MaxConcurrentWorkflows = Math.Max(Environment.ProcessorCount, 4);

        public IServiceCollection Services { get; private set; }

        public WorkflowOptions(IServiceCollection services)
        {
            Services = services;
            PollInterval = TimeSpan.FromSeconds(10);
            IdleTime = TimeSpan.FromMilliseconds(100);
            ErrorRetryInterval = TimeSpan.FromSeconds(60);

            QueueFactory = new Func<IServiceProvider, IQueueProvider>(sp => new SingleNodeQueueProvider());
            LockFactory = new Func<IServiceProvider, IDistributedLockProvider>(sp => new SingleNodeLockProvider());
            PersistanceFactory = new Func<IServiceProvider, IPersistenceProvider>(sp => new TransientMemoryPersistenceProvider(sp.GetService<ISingletonMemoryProvider>()));
            SearchIndexFactory = new Func<IServiceProvider, ISearchIndex>(sp => new NullSearchIndex());
            EventHubFactory = new Func<IServiceProvider, ILifeCycleEventHub>(sp => new SingleNodeEventHub(sp.GetService<ILoggerFactory>()));
        }

        public bool EnableWorkflows { get; set; } = true;
        public bool EnableEvents { get; set; } = true;
        public bool EnableIndexes { get; set; } = true;
        public bool EnablePolling { get; set; } = true;

        public void UsePersistence(Func<IServiceProvider, IPersistenceProvider> factory)
        {
            PersistanceFactory = factory;
        }

        public void UseDistributedLockManager(Func<IServiceProvider, IDistributedLockProvider> factory)
        {
            LockFactory = factory;
        }

        public void UseQueueProvider(Func<IServiceProvider, IQueueProvider> factory)
        {
            QueueFactory = factory;
        }

        public void UseEventHub(Func<IServiceProvider, ILifeCycleEventHub> factory)
        {
            EventHubFactory = factory;
        }

        public void UseSearchIndex(Func<IServiceProvider, ISearchIndex> factory)
        {
            SearchIndexFactory = factory;
        }

        public void UsePollInterval(TimeSpan interval)
        {
            PollInterval = interval;
        }

        public void UseErrorRetryInterval(TimeSpan interval)
        {
            ErrorRetryInterval = interval;
        }

        public void UseMaxConcurrentWorkflows(int maxConcurrentWorkflows)
        {
            MaxConcurrentWorkflows = maxConcurrentWorkflows;
        }
    }
        
}
