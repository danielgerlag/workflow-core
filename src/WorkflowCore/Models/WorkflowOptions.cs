using Microsoft.Extensions.DependencyInjection;
using System;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Models
{
    public class WorkflowOptions
    {
        public Func<IServiceProvider, IPersistenceProvider> PersistanceFactory;
        public Func<IServiceProvider, IQueueProvider> QueueFactory;
        public Func<IServiceProvider, IDistributedLockProvider> LockFactory;
        public TimeSpan PollInterval;
        public TimeSpan IdleTime;
        public TimeSpan ErrorRetryInterval;

        public IServiceCollection Services { get; private set; }

        public WorkflowOptions(IServiceCollection services)
        {
            Services = services;
            PollInterval = TimeSpan.FromSeconds(10);
            IdleTime = TimeSpan.FromMilliseconds(100);
            ErrorRetryInterval = TimeSpan.FromSeconds(60);            

            QueueFactory = new Func<IServiceProvider, IQueueProvider>(sp => new SingleNodeQueueProvider());
            LockFactory = new Func<IServiceProvider, IDistributedLockProvider>(sp => new SingleNodeLockProvider());
            PersistanceFactory = new Func<IServiceProvider, IPersistenceProvider>(sp => new MemoryPersistenceProvider());
        }

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

        public void UsePollInterval(TimeSpan interval)
        {
            PollInterval = interval;
        }

        public void UseErrorRetryInterval(TimeSpan interval)
        {
            ErrorRetryInterval = interval;
        }
    }
        
}
