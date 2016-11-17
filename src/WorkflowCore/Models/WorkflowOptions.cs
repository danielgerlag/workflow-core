using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Models
{
    public class WorkflowOptions
    {
        internal Func<IServiceProvider, IPersistenceProvider> PersistanceFactory;
        internal Func<IServiceProvider, IConcurrencyProvider> ConcurrencyFactory;
        internal int ThreadCount;
        internal TimeSpan PollInterval;
        internal TimeSpan IdleTime;
        internal TimeSpan ErrorRetryInterval;

        public WorkflowOptions()
        {
            //set defaults
            ThreadCount = Environment.ProcessorCount;
            PollInterval = TimeSpan.FromSeconds(10);
            IdleTime = TimeSpan.FromMilliseconds(500);
            ErrorRetryInterval = TimeSpan.FromSeconds(60);

            ConcurrencyFactory = new Func<IServiceProvider, IConcurrencyProvider>(sp => new SingleNodeConcurrencyProvider());
            PersistanceFactory = new Func<IServiceProvider, IPersistenceProvider>(sp => new MemoryPersistenceProvider());
        }

        public void UsePersistence(Func<IServiceProvider, IPersistenceProvider> factory)
        {
            PersistanceFactory = factory;
        }

        public void UseThreads(int count)
        {
            ThreadCount = count;
        }

        public void UsePollInterval(TimeSpan interval)
        {
            PollInterval = interval;
        }

    }
}
