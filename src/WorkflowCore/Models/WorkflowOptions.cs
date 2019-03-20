﻿using Microsoft.Extensions.DependencyInjection;
using System;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Models
{
    public class WorkflowOptions
    {
        internal Func<IServiceProvider, IPersistenceProvider> PersistanceFactory;
        internal Func<IServiceProvider, IQueueProvider> QueueFactory;
        internal Func<IServiceProvider, IDistributedLockProvider> LockFactory;
        internal TimeSpan PollInterval;
        internal TimeSpan IdleTime;
        internal TimeSpan ErrorRetryInterval;
        internal int MaxConcurrentItemsMultiplier;

        public IServiceCollection Services { get; private set; }

        public WorkflowOptions(IServiceCollection services)
        {
            Services = services;
            PollInterval = TimeSpan.FromSeconds(10);
            IdleTime = TimeSpan.FromMilliseconds(100);
            ErrorRetryInterval = TimeSpan.FromSeconds(60);
            MaxConcurrentItemsMultiplier = 1;

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
        
        public void UseMaxConcurrentItemsMultiplier(int multiplier)
        {
            MaxConcurrentItemsMultiplier = multiplier > 0 && multiplier <= 10 ? multiplier 
            : throw new ArgumentOutOfRangeException($"{nameof(multiplier)} must be a value from 1 through 10");
        }
    }
        
}
